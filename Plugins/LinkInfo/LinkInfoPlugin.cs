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

        protected ConnectionManager ConnectionManager;

        private Uri _lastLink = null;

        public LinkInfoPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;

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
                if (_lastLink == null)
                {
                    ConnectionManager.SendChannelMessage(args.Data.Channel, "No last link!");
                }
                else
                {
                    PostLinkInfo(new [] {_lastLink}, args.Data.Channel);
                }
                return;
            }

            // find all the links
            var links = FindLinks(body);

            // store the new "last link"
            if (links.Count > 0)
            {
                _lastLink = links[links.Count - 1];
            }

            // respond?
            if (body.StartsWith("!link "))
            {
                PostLinkInfo(links, args.Data.Channel);
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
                    ret.Add(uri);
                }
            }
            return ret;
        }

        public static string RealObtainLinkInfo(Uri link)
        {
            var lowerUrl = link.ToString().ToLowerInvariant();
            if (!lowerUrl.StartsWith("http://") && !lowerUrl.StartsWith("https://"))
            {
                return "(I only access HTTP and HTTPS URLs)";
            }

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
                    var resp = request.GetResponse();

                    // find the content-type
                    contentTypeHeader = resp.Headers[HttpResponseHeader.ContentType];
                    if (contentTypeHeader != null)
                    {
                        contentType = contentTypeHeader.Split(';')[0];
                    }
                    var webResp = resp as HttpWebResponse;
                    responseCharacterSet = (webResp != null) ? webResp.CharacterSet : null;

                    // copy
                    resp.GetResponseStream().CopyTo(respStore);
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

        public static string ObtainLinkInfo(Uri link)
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

        protected void PostLinkInfo(IEnumerable<Uri> links, string channel)
        {
            foreach (var linkAndInfo in links.Select(l => new LinkAndInfo(l, ObtainLinkInfo(l))))
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
}
