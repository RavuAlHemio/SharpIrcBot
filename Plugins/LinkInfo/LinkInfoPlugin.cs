using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using JetBrains.Annotations;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Chunks;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;

namespace LinkInfo
{
    public class LinkInfoPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string GoogleHomepageUrlPattern = "https://{0}/";
        public const string GoogleImageSearchUrlPattern = "https://{0}/imghp?hl=en&tab=wi";
        public const string GoogleImageSearchByImageUrlPattern = "https://{0}/searchbyimage?hl=en&image_url={1}";
        public const int DownloadBufferSize = 4 * 1024 * 1024;
        public static readonly Regex WhiteSpaceRegex = new Regex("\\s+", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager { get; set; }
        protected LinkInfoConfig Config { get; set; }

        [CanBeNull]
        protected LinkAndInfo LastLinkAndInfo { get; set; }
        [CanBeNull]
        protected HashSet<string> TopLevelDomainCache { get; set; }
        [NotNull]
        protected static IdnMapping IDNMapping { get; set; }

        static LinkInfoPlugin()
        {
            IDNMapping = new IdnMapping();
        }

        public LinkInfoPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new LinkInfoConfig(config);

            LastLinkAndInfo = null;
            TopLevelDomainCache = null;

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.OutgoingChannelMessage += HandleOutgoingChannelMessage;
            ConnectionManager.SplitToChunks += HandleSplitToChunks;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new LinkInfoConfig(newConfig);
        }

        protected void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var body = args.Message;
            if (body == "!lastlink" || body == "!ll")
            {
                if (LastLinkAndInfo == null)
                {
                    ConnectionManager.SendChannelMessage(args.Channel, "No last link!");
                }
                else
                {
                    // fetch if not fetched yet or a transient error occurred last time
                    if (LastLinkAndInfo.ShouldRefetch)
                    {
                        LastLinkAndInfo = ObtainLinkInfo(LastLinkAndInfo.Link);
                    }
                    PostLinkInfoToChannel(LastLinkAndInfo, args.Channel);
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

            // do something with the links
            LinksAction(args, flags, links);
        }

        protected virtual void LinksAction(IChannelMessageEventArgs args, MessageFlags flags, IList<Uri> links)
        {
            // respond?
            if (Config.AutoShowLinkInfo || args.Message.StartsWith("!link "))
            {
                foreach (var linkAndInfo in links.Select(ObtainLinkInfo))
                {
                    PostLinkInfoToChannel(linkAndInfo, args.Channel);
                }
            }
        }

        protected void HandleOutgoingChannelMessage(object sender, OutgoingMessageEventArgs args)
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

        public IList<Uri> FindLinks(string message)
        {
            return ConnectionManager
                .SplitMessageToChunks(message)
                .OfType<UriMessageChunk>()
                .Select(umc => umc.Uri)
                .ToList();
        }

        public static string FoldWhitespace(string str)
        {
            return WhiteSpaceRegex.Replace(str, " ");
        }

        [NotNull]
        public virtual LinkAndInfo RealObtainLinkInfo([NotNull] Uri link, [CanBeNull] Uri originalLink = null, int redirectCount = 0)
        {
            // hyperrecursion?
            if (redirectCount > Config.MaxRedirects)
            {
                return new LinkAndInfo(link, "(too many redirections)", FetchErrorLevel.TransientError, originalLink);
            }

            // check URL blacklist
            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostAddresses(link.Host);
            }
            catch (SocketException se)
            {
                Logger.WarnFormat("socket exception when resolving {0}: {1}", link.Host, se);
                return new LinkAndInfo(link, "(cannot resolve)", FetchErrorLevel.TransientError, originalLink);
            }

            if (addresses.Length == 0)
            {
                Logger.WarnFormat("no addresses found when resolving {0}", link.Host);
                return new LinkAndInfo(link, "(cannot resolve)", FetchErrorLevel.TransientError, originalLink);
            }
            if (addresses.Any(IPAddressBlacklist.IsIPAddressBlacklisted))
            {
                return new LinkAndInfo(link, "(I refuse to access this IP address)", FetchErrorLevel.LastingError, originalLink);
            }

            var request = WebRequest.Create(link);
            var httpRequest = request as HttpWebRequest;
            using (var respStore = new MemoryStream())
            {
                var contentType = "application/octet-stream";
                string contentTypeHeader = null;
                request.Timeout = (int)TimeSpan.FromSeconds(Config.TimeoutSeconds).TotalMilliseconds;
                if (httpRequest != null)
                {
                    // HTTP-specific settings
                    httpRequest.AllowAutoRedirect = false;
                    httpRequest.UserAgent = Config.FakeUserAgent;
                }

                try
                {
                    using (var resp = request.GetResponse())
                    {
                        // redirect?
                        string location = resp.Headers[HttpResponseHeader.Location];
                        if (location != null)
                        {
                            // go there instead
                            Logger.Debug($"{link.AbsoluteUri} (originally {originalLink?.AbsoluteUri ?? link.AbsoluteUri}) redirects to {location}");
                            return RealObtainLinkInfo(new Uri(location), originalLink ?? link, redirectCount + 1);
                        }

                        // find the content-type
                        contentTypeHeader = resp.Headers[HttpResponseHeader.ContentType];
                        if (contentTypeHeader != null)
                        {
                            contentType = contentTypeHeader.Split(';')[0];
                        }

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
                                return new LinkAndInfo(link, "(file too large)", FetchErrorLevel.LastingError, originalLink);
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
                        return new LinkAndInfo(link, $"(HTTP {httpResponse.StatusCode})", FetchErrorLevel.TransientError, originalLink);
                    }
                    Logger.Warn("HTTP exception thrown", we);
                    return new LinkAndInfo(link, "(HTTP error)", FetchErrorLevel.TransientError, originalLink);
                }

                switch (contentType)
                {
                    case "application/octet-stream":
                        return new LinkAndInfo(link, "(can't figure out the content type, sorry)", FetchErrorLevel.LastingError, originalLink);
                    case "text/html":
                    case "application/xhtml+xml":
                        // HTML? parse it and get the title
                        var respStr = EncodingGuesser.GuessEncodingAndDecode(respStore.ToArray(), contentTypeHeader);

                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(respStr);
                        var titleElement = htmlDoc.DocumentNode.SelectSingleNode(".//title");
                        if (titleElement != null)
                        {
                            return new LinkAndInfo(link, FoldWhitespace(HtmlEntity.DeEntitize(titleElement.InnerText)), FetchErrorLevel.Success, originalLink);
                        }
                        var h1Element = htmlDoc.DocumentNode.SelectSingleNode(".//h1");
                        if (h1Element != null)
                        {
                            return new LinkAndInfo(link, FoldWhitespace(HtmlEntity.DeEntitize(h1Element.InnerText)), FetchErrorLevel.Success, originalLink);
                        }
                        return new LinkAndInfo(link, "(HTML without a title O_o)", FetchErrorLevel.Success, originalLink);
                    case "image/png":
                        return new LinkAndInfo(link, ObtainImageInfo(link, "PNG image"), FetchErrorLevel.Success, originalLink);
                    case "image/jpeg":
                        return new LinkAndInfo(link, ObtainImageInfo(link, "JPEG image"), FetchErrorLevel.Success, originalLink);
                    case "image/gif":
                        return new LinkAndInfo(link, ObtainImageInfo(link, "GIF image"), FetchErrorLevel.Success, originalLink);
                    case "application/json":
                        return new LinkAndInfo(link, "JSON", FetchErrorLevel.Success, originalLink);
                    case "text/xml":
                    case "application/xml":
                        return new LinkAndInfo(link, "XML", FetchErrorLevel.Success, originalLink);
                    default:
                        return new LinkAndInfo(link, $"file of type {contentType}", FetchErrorLevel.Success, originalLink);
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
                    url.AbsoluteUri
                ));
                client.Headers[HttpRequestHeader.UserAgent] = Config.FakeUserAgent;
                client.Headers[HttpRequestHeader.Referer] = googleImageSearchUrl;
                var responseBytes = client.DownloadData(searchUrl);
                var parseMe = EncodingGuesser.GuessEncodingAndDecode(responseBytes, null);
                
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
                return new LinkAndInfo(link, "(an error occurred)", FetchErrorLevel.TransientError, null);
            }
        }

        protected void PostLinkInfoToChannel(LinkAndInfo linkAndInfo, string channel)
        {
            PostLinkInfo(linkAndInfo, message => ConnectionManager.SendChannelMessage(channel, message));
        }

        protected void PostLinkInfo(LinkAndInfo linkAndInfo, Action<string> post)
        {
            string linkString = linkAndInfo.Link.AbsoluteUri;
            string info = linkAndInfo.Info;
            if (linkAndInfo.ErrorLevel == FetchErrorLevel.Success && Config.FakeResponses.ContainsKey(linkString))
            {
                info = Config.FakeResponses[linkString];
            }

            string redirectedString = (linkAndInfo.OriginalLink != null)
                ? $"{linkAndInfo.OriginalLink.AbsoluteUri} -> "
                : "";

            post($"{redirectedString}{linkString} {(linkAndInfo.IsError ? ":!:" : "::")} {info}");
        }

        protected bool TryCreateUriHeuristically(string word, out Uri uri)
        {
            uri = null;

            if (TopLevelDomainCache == null)
            {
                if (Config.TLDListFile == null)
                {
                    return false;
                }

                string tldListFilePath = Path.Combine(SharpIrcBotUtil.AppDirectory, Config.TLDListFile);
                if (!File.Exists(tldListFilePath))
                {
                    return false;
                }

                TopLevelDomainCache = new HashSet<string>();
                using (var reader = new StreamReader(tldListFilePath, SharpIrcBotUtil.Utf8NoBom))
                {
                    string line;
                    while ((line = reader.ReadLine()?.Trim()) != null)
                    {
                        if (line.StartsWith("#"))
                        {
                            continue;
                        }
                        TopLevelDomainCache.Add(line.ToLowerInvariant());
                    }
                }
            }

            // fail fast for obvious non-URLs
            if (word.All(c => c != '.' && c != '/'))
            {
                return false;
            }

            // would this word make sense with http:// in front of it?
            if (!Uri.TryCreate("http://" + word, UriKind.Absolute, out uri))
            {
                // nope
                return false;
            }

            // does the host have at least one dot?
            if (uri.Host.All(c => c != '.'))
            {
                return false;
            }

            // check host against list of TLDs
            var tld = IDNMapping.GetAscii(uri.Host.Split('.').Last()).ToLowerInvariant();
            if (!TopLevelDomainCache.Contains(tld))
            {
                // invalid TLD; probably not a URI
                uri = null;
                return false;
            }

            // it probably is a URI
            return true;
        }

        protected void HandleSplitToChunks(object sender, MessageChunkingEventArgs e)
        {
            var newChunks = new List<IMessageChunk>();

            foreach (IMessageChunk chunk in e.Chunks)
            {
                if (!(chunk is TextMessageChunk))
                {
                    newChunks.Add(chunk);
                    continue;
                }

                var textChunk = (TextMessageChunk) chunk;

                var wordChunks = new List<IMessageChunk>();
                foreach (string word in textChunk.Text.Split(' '))
                {
                    Uri uri;
                    if (Uri.TryCreate(word, UriKind.Absolute, out uri))
                    {
                        if (uri.Scheme != "http" && uri.Scheme != "https")
                        {
                            continue;
                        }
                        // Uri verifies that http(s) URIs are at least minimal (http://a)
                        wordChunks.Add(new UriMessageChunk(word, uri));
                    }
                    else if (TryCreateUriHeuristically(word, out uri))
                    {
                        wordChunks.Add(new UriMessageChunk(word, uri));
                    }
                    else
                    {
                        wordChunks.Add(new TextMessageChunk(word));
                    }
                }

                var spaceChunk = new TextMessageChunk(" ");
                for (int i = 0; i < wordChunks.Count; ++i)
                {
                    newChunks.Add(wordChunks[i]);
                    if (i < wordChunks.Count - 1)
                    {
                        newChunks.Add(spaceChunk);
                    }
                }
            }

            e.Chunks = SharpIrcBotUtil.SimplifyAdjacentTextChunks(newChunks);
        }
    }
}
