using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace LinkInfo
{
    public class LinkInfoPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string GoogleHomepageUrlPattern = "https://{0}/";
        public const string GoogleImageSearchUrlPattern = "https://{0}/imghp?hl=en&tab=wi";
        public const string GoogleImageSearchByImageUrlPattern = "https://{0}/searchbyimage?hl=en&image_url={1}";
        public const int DownloadBufferSize = 4 * 1024 * 1024;
        public static readonly Regex WhiteSpaceRegex = new Regex("\\s+");

        protected ConnectionManager ConnectionManager { get; set; }
        protected LinkInfoConfig Config { get; set; }

        protected LinkAndInfo LastLinkAndInfo { get; set; }

        public LinkInfoPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new LinkInfoConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.OutgoingChannelMessage += HandleOutgoingChannelMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new LinkInfoConfig(newConfig);
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
            if (body == "!lastlink" || body == "!ll")
            {
                if (LastLinkAndInfo == null)
                {
                    ConnectionManager.SendChannelMessage(args.Data.Channel, "No last link!");
                }
                else
                {
                    // fetch if not fetched yet or a transient error occurred last time
                    if (LastLinkAndInfo.ShouldRefetch)
                    {
                        LastLinkAndInfo = ObtainLinkInfo(LastLinkAndInfo.Link);
                    }
                    PostLinkInfo(LastLinkAndInfo, args.Data.Channel);
                }
                return;
            }

            // find all the links
            var links = FindLinks(body);

            // store the new "last link"
            if (links.Count > 0)
            {
                LastLinkAndInfo = LinkAndInfo.CreateUnfetched(links[links.Count-1]);
            }

            // respond?
            if (Config.AutoShowLinkInfo || body.StartsWith("!link "))
            {
                FetchAndPostLinkInfo(links, args.Data.Channel);
            }
        }

        private void HandleOutgoingChannelMessage(object sender, OutgoingMessageEventArgs args)
        {
            try
            {
                ActuallyHandleOutgoingChannelMessage(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling outgoing message", exc);
            }
        }

        protected void ActuallyHandleOutgoingChannelMessage(object sender, OutgoingMessageEventArgs args)
        {
            // find all the links
            var links = FindLinks(args.OutgoingMessage);

            // store the new "last link" unless it's already cached
            if (links.Count > 0)
            {
                var lastLink = links[links.Count - 1];
                if (LastLinkAndInfo == null || LastLinkAndInfo.Link != lastLink)
                {
                    LastLinkAndInfo = LinkAndInfo.CreateUnfetched(lastLink);
                }
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

        public static string FoldWhitespace(string str)
        {
            return WhiteSpaceRegex.Replace(str, " ");
        }

        public LinkAndInfo RealObtainLinkInfo(Uri link)
        {
            // check URL blacklist
            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostAddresses(link.Host);
            }
            catch (SocketException se)
            {
                Logger.WarnFormat("socket exception when resolving {0}: {1}", link.Host, se);
                return new LinkAndInfo(link, "(cannot resolve)", FetchErrorLevel.TransientError);
            }

            if (addresses.Length == 0)
            {
                Logger.WarnFormat("no addresses found when resolving {0}", link.Host);
                return new LinkAndInfo(link, "(cannot resolve)", FetchErrorLevel.TransientError);
            }
            if (addresses.Any(IPAddressBlacklist.IsIPAddressBlacklisted))
            {
                return new LinkAndInfo(link, "(I refuse to access this IP address)", FetchErrorLevel.LastingError);
            }

            var request = WebRequest.Create(link);
            var httpRequest = request as HttpWebRequest;
            using (var respStore = new MemoryStream())
            {
                var contentType = "application/octet-stream";
                string contentTypeHeader = null;
                string responseCharacterSet = null;
                request.Timeout = (int)TimeSpan.FromSeconds(Config.TimeoutSeconds).TotalMilliseconds;
                if (httpRequest != null)
                {
                    // HTTP-specific settings
                    httpRequest.UserAgent = Config.FakeUserAgent;
                }

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
                        responseCharacterSet = webResp?.CharacterSet;

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
                                return new LinkAndInfo(link, "(file too large)", FetchErrorLevel.LastingError);
                            }
                            respStore.Write(buf, 0, bytesRead);
                        }
                    }
                }
                catch (WebException we)
                {
                    var httpResponse = we.Response as HttpWebResponse;
                    if (httpResponse != null)
                    {
                        return new LinkAndInfo(link, $"(HTTP {httpResponse.StatusCode})", FetchErrorLevel.TransientError);
                    }
                    Logger.Warn("HTTP exception thrown", we);
                    return new LinkAndInfo(link, "(HTTP error)", FetchErrorLevel.TransientError);
                }

                switch (contentType)
                {
                    case "application/octet-stream":
                        return new LinkAndInfo(link, "(can't figure out the content type, sorry)", FetchErrorLevel.LastingError);
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
                            return new LinkAndInfo(link, FoldWhitespace(HtmlEntity.DeEntitize(titleElement.InnerText)), FetchErrorLevel.Success);
                        }
                        var h1Element = htmlDoc.DocumentNode.SelectSingleNode(".//h1");
                        if (h1Element != null)
                        {
                            return new LinkAndInfo(link, FoldWhitespace(HtmlEntity.DeEntitize(h1Element.InnerText)), FetchErrorLevel.Success);
                        }
                        return new LinkAndInfo(link, "(HTML without a title O_o)", FetchErrorLevel.Success);
                    case "image/png":
                        return new LinkAndInfo(link, ObtainImageInfo(link, "PNG image"), FetchErrorLevel.Success);
                    case "image/jpeg":
                        return new LinkAndInfo(link, ObtainImageInfo(link, "JPEG image"), FetchErrorLevel.Success);
                    case "image/gif":
                        return new LinkAndInfo(link, ObtainImageInfo(link, "GIF image"), FetchErrorLevel.Success);
                    case "application/json":
                        return new LinkAndInfo(link, "JSON", FetchErrorLevel.Success);
                    case "text/xml":
                    case "application/xml":
                        return new LinkAndInfo(link, "XML", FetchErrorLevel.Success);
                    default:
                        return new LinkAndInfo(link, $"file of type {contentType}", FetchErrorLevel.Success);
                }
            }
        }

        public string ObtainImageInfo(Uri url, string text)
        {
            try
            {
                var client = new CookieWebClient
                {
                    Timeout = TimeSpan.FromSeconds(Config.ImageInfoTimeoutSeconds)
                };

                var googleImageSearchUrl = string.Format(GoogleImageSearchUrlPattern, Config.GoogleDomain);

                // alibi-visit the image search page to get the cookies
                client.Headers[HttpRequestHeader.UserAgent] = Config.FakeUserAgent;
                client.Headers[HttpRequestHeader.Referer] = string.Format(GoogleHomepageUrlPattern, Config.GoogleDomain);
                client.DownloadData(googleImageSearchUrl);

                // fetch the actual info
                var searchUrl = new Uri(string.Format(
                    GoogleImageSearchByImageUrlPattern,
                    Config.GoogleDomain,
                    SharpIrcBotUtil.UrlEncode(url.ToString(), SharpIrcBotUtil.Utf8NoBom, true)
                ));
                client.Headers[HttpRequestHeader.UserAgent] = Config.FakeUserAgent;
                client.Headers[HttpRequestHeader.Referer] = googleImageSearchUrl;
                var responseBytes = client.DownloadData(searchUrl);
                var parseMe = EncodingGuesser.GuessEncodingAndDecode(responseBytes, null, null);
                
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(parseMe);
                var foundHints = htmlDoc.DocumentNode.QuerySelectorAll("._hUb ._gUb");
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

        public LinkAndInfo ObtainLinkInfo(Uri link)
        {
            try
            {
                return RealObtainLinkInfo(link);
            }
            catch (Exception ex)
            {
                Logger.Warn("link info", ex);
                return new LinkAndInfo(link, "(an error occurred)", FetchErrorLevel.TransientError);
            }
        }

        protected void FetchAndPostLinkInfo(IEnumerable<Uri> links, string channel)
        {
            foreach (var linkAndInfo in links.Select(ObtainLinkInfo))
            {
                PostLinkInfo(linkAndInfo, channel);
            }
        }

        protected void PostLinkInfo(LinkAndInfo linkAndInfo, string channel)
        {
            string linkString = linkAndInfo.Link.ToString();
            string info = linkAndInfo.Info;
            if (linkAndInfo.ErrorLevel == FetchErrorLevel.Success && Config.FakeResponses.ContainsKey(linkString))
            {
                info = Config.FakeResponses[linkString];
            }

            ConnectionManager.SendChannelMessageFormat(
                channel,
                "{0} {2} {1}",
                linkString,
                info,
                linkAndInfo.IsError ? ":!:" : "::"
            );
        }
    }
}
