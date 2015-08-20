using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace LinkInfo
{
    public class LinkInfoPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly Uri GoogleHomepageUrl = new Uri("http://www.google.com/");
        public static readonly Uri GoogleImageSearchUrl = new Uri("http://www.google.com/imghp?hl=en&tab=wi");
        public const string GoogleImageSearchByImageUrlPattern = "https://www.google.com/searchbyimage?hl=en&image_url={0}";
        public const string FakeUserAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:31.0) Gecko/20100101 Firefox/31.0";
        public const int DownloadBufferSize = 4 * 1024 * 1024;

        protected ConnectionManager ConnectionManager;
        protected LinkInfoConfig Config;

        private LinkAndInfo _lastLinkAndInfo = null;

        public LinkInfoPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new LinkInfoConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected void ActuallyHandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var body = args.Data.Message;
            if (body == "!lastlink")
            {
                if (_lastLinkAndInfo == null)
                {
                    ConnectionManager.SendChannelMessage(args.Data.Channel, "No last link!");
                }
                else
                {
                    // fetch if not fetched yet
                    if (!_lastLinkAndInfo.HasInfo)
                    {
                        var info = ObtainLinkInfo(_lastLinkAndInfo.Link);
                        _lastLinkAndInfo = new LinkAndInfo(_lastLinkAndInfo.Link, info);
                    }
                    PostLinkInfo(_lastLinkAndInfo, args.Data.Channel);
                }
                return;
            }

            // find all the links
            var links = FindLinks(body);

            // store the new "last link"
            if (links.Count > 0)
            {
                _lastLinkAndInfo = new LinkAndInfo(links[links.Count - 1], null);
            }

            // respond?
            if (body.StartsWith("!link "))
            {
                FetchAndPostLinkInfo(links, args.Data.Channel);
            }
        }

        public static IList<Uri> FindLinks(string message)
        {
            var ret = new List<Uri>();
            foreach (var word in message.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries))
            {
                Uri uri;
                if (Uri.TryCreate(word, UriKind.Absolute, out uri))
                {
                    if (uri.Scheme != "http" && uri.Scheme != "https")
                    {
                        continue;
                    }
                    // Uri verifies that http(s) URIs are at least minimal (http://a)
                    ret.Add(uri);
                }
            }
            return ret;                
        }

        public string RealObtainLinkInfo(Uri link)
        {
            // check URL blacklist
            var addresses = Dns.GetHostAddresses(link.Host);
            if (addresses.Length == 0)
            {
                return "(cannot resolve)";
            }
            if (addresses.Any(IPAddressBlacklist.IsIPAddressBlacklisted))
            {
                return "(I refuse to access this IP address)";
            }

            var request = WebRequest.Create(link);
            using (var respStore = new MemoryStream())
            {
                var contentType = "application/octet-stream";
                string contentTypeHeader = null;
                string responseCharacterSet = null;
                request.Timeout = 5000;
                try
                {
                    using (var resp = request.GetResponse())
                    {
                        // find the content-type
                        contentTypeHeader = resp.Headers[HttpResponseHeader.ContentType];
                        if (contentTypeHeader != null)
                        {
                            contentType = contentTypeHeader.Split(';')[0];
                        }
                        var webResp = resp as HttpWebResponse;
                        responseCharacterSet = (webResp != null) ? webResp.CharacterSet : null;

                        // copy
                        var buf = new byte[DownloadBufferSize];
                        var responseStream = resp.GetResponseStream();
                        long totalBytesRead = 0;
                        for (;;)
                        {
                            int bytesRead = responseStream.Read(buf, 0, buf.Length);
                            if (bytesRead == 0)
                            {
                                break;
                            }
                            totalBytesRead += bytesRead;
                            if (totalBytesRead > Config.MaxDownloadSizeBytes)
                            {
                                return "(file too large)";
                            }
                            respStore.Write(buf, 0, bytesRead);
                        }
                    }
                }
                catch (WebException we)
                {
                    var httpResponse = we.Response as HttpWebResponse;
                    return string.Format("(HTTP {0})", httpResponse != null ? httpResponse.StatusCode.ToString() : "error");
                }

                switch (contentType)
                {
                    case "application/octet-stream":
                        return "(can't figure out the content type, sorry)";
                    case "text/html":
                    case "application/xhtml+xml":
                        // HTML? parse it and get the title
                        var respStr = EncodingGuesser.GuessEncodingAndDecode(respStore.ToArray(), responseCharacterSet,
                                      contentTypeHeader);

                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(respStr);
                        var titleElement = htmlDoc.DocumentNode.SelectSingleNode(".//title");
                        if (titleElement != null)
                        {
                            return HtmlEntity.DeEntitize(titleElement.InnerText);
                        }
                        var h1Element = htmlDoc.DocumentNode.SelectSingleNode(".//h1");
                        if (h1Element != null)
                        {
                            return HtmlEntity.DeEntitize(h1Element.InnerText);
                        }
                        return "(HTML without a title O_o)";
                    case "image/png":
                        return ObtainImageInfo(link, "PNG image");
                    case "image/jpeg":
                        return ObtainImageInfo(link, "JPEG image");
                    case "image/gif":
                        return ObtainImageInfo(link, "GIF image");
                    case "application/json":
                        return "JSON";
                    case "text/xml":
                    case "application/xml":
                        return "XML";
                    default:
                        return string.Format("file of type {0}", contentType);
                }
            }
        }

        public static string ObtainImageInfo(Uri url, string text)
        {
            try
            {
                var client = new CookieWebClient();
                client.Headers[HttpRequestHeader.UserAgent] = FakeUserAgent;

                // alibi-visit the image search page to get the cookies
                client.Headers[HttpRequestHeader.Referer] = GoogleHomepageUrl.ToString();
                client.DownloadData(GoogleImageSearchUrl);

                // fetch the actual info
                var searchUrl = new Uri(string.Format(
                    GoogleImageSearchByImageUrlPattern,
                    SharpIrcBotUtil.UrlEncode(url.ToString(), SharpIrcBotUtil.Utf8NoBom, true)
                ));
                client.Headers[HttpRequestHeader.Referer] = GoogleImageSearchUrl.ToString();
                var responseBytes = client.DownloadData(searchUrl);
                var parseMe = EncodingGuesser.GuessEncodingAndDecode(responseBytes, null, null);
                
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(parseMe);
                var foundHints = htmlDoc.DocumentNode.QuerySelectorAll(".qb-bmqc .qb-b");
                foreach (var hint in foundHints)
                {
                    return string.Format("{0} ({1})", text, HtmlEntity.DeEntitize(hint.InnerText));
                }
                return text;
            }
            catch (Exception ex)
            {
                Logger.Warn("image info", ex);
                return text;
            }
        }

        public string ObtainLinkInfo(Uri link)
        {
            try
            {
                return RealObtainLinkInfo(link);
            }
            catch (Exception ex)
            {
                Logger.Warn("link info", ex);
                return "(an error occurred)";
            }
        }

        protected void FetchAndPostLinkInfo(IEnumerable<Uri> links, string channel)
        {
            foreach (var linkAndInfo in links.Select(l => new LinkAndInfo(l, ObtainLinkInfo(l))))
            {
                PostLinkInfo(linkAndInfo, channel);
            }
        }

        protected void PostLinkInfo(LinkAndInfo linkAndInfo, string channel)
        {
            ConnectionManager.SendChannelMessageFormat(
                channel,
                "{0}: {1}",
                linkAndInfo.Link,
                linkAndInfo.Info
            );
        }
    }
}
