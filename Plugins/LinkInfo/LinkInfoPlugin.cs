using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Chunks;
using SharpIrcBot.Commands;
using SharpIrcBot.Config;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.LinkInfo
{
    public class LinkInfoPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<LinkInfoPlugin>();

        public const int DownloadBufferSize = 4 * 1024 * 1024;

        protected IConnectionManager ConnectionManager { get; set; }
        protected LinkInfoConfig Config { get; set; }
        protected IdnMapping IDNMapping { get; set; }
        protected List<ILinkResolverPlugin> Plugins { get; set; }

        [CanBeNull]
        protected LinkAndInfo LastLinkAndInfo { get; set; }
        [CanBeNull]
        protected HeuristicLinkDetector LinkDetector { get; set; }

        protected string LinkCommandPrefix => $"{ConnectionManager.CommandManager.Config.CommandPrefix}link ";

        public LinkInfoPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new LinkInfoConfig(config);
            IDNMapping = new IdnMapping();
            Plugins = new List<ILinkResolverPlugin>();

            LastLinkAndInfo = null;
            LinkDetector = null;

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.OutgoingChannelMessage += HandleOutgoingChannelMessage;
            ConnectionManager.SplitToChunks += HandleSplitToChunks;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("ll", "lastlink"),
                    CommandUtil.NoOptions,
                    CommandUtil.NoArguments,
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleLastLinkCommand
            );

            RepopulatePluginList();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new LinkInfoConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            // recreate heuristic link detector at next use
            LinkDetector = null;

            RepopulatePluginList();
        }

        protected virtual void RepopulatePluginList()
        {
            Plugins.Clear();

            foreach (PluginConfig config in Config.LinkResolverPlugins.Where(pc => pc.Enabled))
            {
                Assembly ass = Assembly.Load(new AssemblyName(config.Assembly));
                Type type = ass.GetType(config.Class);
                if (!typeof(ILinkResolverPlugin).GetTypeInfo().IsAssignableFrom(type))
                {
                    throw new ArgumentException($"class {type.FullName} is not a link resolver plugin");
                }
                ConstructorInfo ctor = type.GetTypeInfo().GetConstructor(new [] {typeof(JObject), typeof(LinkInfoConfig)});
                var pluginObject = (ILinkResolverPlugin)ctor.Invoke(new object[] {config.Config, this.Config});
                Plugins.Add(pluginObject);
            }
        }

        protected void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            string body = args.Message;

            // find all the links
            IList<Uri> links = FindLinks(body);

            // store the new "last link"
            if (links.Count > 0)
            {
                LastLinkAndInfo = LinkAndInfo.CreateUnfetched(links[links.Count-1]);
            }

            // do something with the links
            LinksAction(args, flags, links);
        }

        protected virtual void HandleLastLinkCommand(CommandMatch cmd, IChannelMessageEventArgs args)
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
        }

        protected virtual void LinksAction(IChannelMessageEventArgs args, MessageFlags flags, IList<Uri> links)
        {
            // respond?
            if (Config.AutoShowLinkInfo || args.Message.StartsWith(LinkCommandPrefix))
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

        [NotNull]
        public virtual LinkAndInfo RealObtainLinkInfo([NotNull] Uri link, [CanBeNull] Uri originalLink = null, int redirectCount = 0)
        {
            // hyperrecursion?
            if (redirectCount > Config.MaxRedirects)
            {
                return new LinkAndInfo(link, "(too many redirections)", FetchErrorLevel.TransientError, originalLink);
            }

            var linkBuilder = new UriBuilder(link);
            linkBuilder.Fragment = "";

            // check URL blacklist
            IPAddress[] addresses;
            try
            {
                linkBuilder.Host = IDNMapping.GetAscii(link.Host);
                addresses = Dns.GetHostAddressesAsync(linkBuilder.Host).SyncWait();
            }
            catch (SocketException se)
            {
                Logger.LogWarning("socket exception when resolving {Host}: {Exception}", linkBuilder.Host, se);
                return new LinkAndInfo(link, "(cannot resolve)", FetchErrorLevel.TransientError, originalLink);
            }

            if (addresses.Length == 0)
            {
                Logger.LogWarning("no addresses found when resolving {Host}", linkBuilder.Host);
                return new LinkAndInfo(link, "(cannot resolve)", FetchErrorLevel.TransientError, originalLink);
            }
            if (addresses.Any(IPAddressBlacklist.IsIPAddressBlacklisted))
            {
                return new LinkAndInfo(link, "(I refuse to access this IP address)", FetchErrorLevel.LastingError, originalLink);
            }

            var httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            using (httpClientHandler)
            using (var httpClient = new HttpClient(httpClientHandler))
            using (var request = new HttpRequestMessage(HttpMethod.Get, linkBuilder.Uri))
            using (var respStore = new MemoryStream())
            {
                var contentType = new MediaTypeHeaderValue("application/octet-stream");

                httpClient.Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds);
                request.Headers.UserAgent.TryParseAdd(Config.FakeUserAgent);
                request.Headers.AcceptLanguage.TryParseAdd(Config.AcceptLanguage);

                using (var resp = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).SyncWait())
                {
                    try
                    {
                        // redirect?
                        Uri location = resp.Headers.Location;
                        if (location != null)
                        {
                            // go there instead
                            Logger.LogDebug(
                                "{AbsoluteURI} (originally {OriginalAbsoluteURI}) redirects to {Location}",
                                link.AbsoluteUri, originalLink?.AbsoluteUri ?? link.AbsoluteUri, location
                            );
                            return RealObtainLinkInfo(new Uri(link, location), originalLink ?? link, redirectCount + 1);
                        }

                        // success?
                        if (!resp.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException("unsuccessful");
                        }

                        // find the content-type
                        contentType = resp.Content.Headers.ContentType ?? contentType;

                        // start timing
                        var readTimeout = TimeSpan.FromSeconds(Config.TimeoutSeconds);
                        var timer = new Stopwatch();
                        timer.Start();

                        // copy
                        var buf = new byte[DownloadBufferSize];
                        Stream responseStream = resp.Content.ReadAsStreamAsync().SyncWait();
                        if (responseStream.CanTimeout)
                        {
                            responseStream.ReadTimeout = (int)readTimeout.TotalMilliseconds;
                        }
                        long totalBytesRead = 0;
                        for (;;)
                        {
                            int bytesRead = responseStream.Read(buf, 0, buf.Length);
                            if (bytesRead == 0)
                            {
                                break;
                            }
                            totalBytesRead += bytesRead;
                            if (timer.Elapsed > readTimeout)
                            {
                                return new LinkAndInfo(link, "(reading timed out)", FetchErrorLevel.TransientError, originalLink);
                            }
                            if (totalBytesRead > Config.MaxDownloadSizeBytes)
                            {
                                return new LinkAndInfo(link, "(file too large)", FetchErrorLevel.LastingError, originalLink);
                            }
                            respStore.Write(buf, 0, bytesRead);
                        }
                    }
                    catch (HttpRequestException we)
                    {
                        if (resp != null)
                        {
                            return new LinkAndInfo(link, $"(HTTP {resp.StatusCode})", FetchErrorLevel.TransientError, originalLink);
                        }
                        Logger.LogWarning("HTTP exception thrown: {Exception}", we);
                        return new LinkAndInfo(link, "(HTTP error)", FetchErrorLevel.TransientError, originalLink);
                    }
                }

                var linkToResolve = new LinkToResolve(link, originalLink, respStore.ToArray(), contentType);
                foreach (ILinkResolverPlugin plugin in Plugins)
                {
                    LinkAndInfo ret = plugin.ResolveLink(linkToResolve);
                    if (ret != null)
                    {
                        return ret;
                    }
                }

                // fallback
                switch (contentType.MediaType)
                {
                    case "application/octet-stream":
                        return new LinkAndInfo(link, "(can't figure out the content type, sorry)", FetchErrorLevel.LastingError, originalLink);
                    case "text/html":
                    case "application/xhtml+xml":
                        return new LinkAndInfo(link, "HTML", FetchErrorLevel.Success, originalLink);
                    case "image/png":
                        return new LinkAndInfo(link, "PNG image", FetchErrorLevel.Success, originalLink);
                    case "image/jpeg":
                        return new LinkAndInfo(link, "JPEG image", FetchErrorLevel.Success, originalLink);
                    case "image/gif":
                        return new LinkAndInfo(link, "GIF image", FetchErrorLevel.Success, originalLink);
                    case "application/json":
                        return new LinkAndInfo(link, "JSON", FetchErrorLevel.Success, originalLink);
                    case "text/xml":
                    case "application/xml":
                        return new LinkAndInfo(link, "XML", FetchErrorLevel.Success, originalLink);
                    default:
                        return new LinkAndInfo(link, $"file of type {contentType.MediaType}", FetchErrorLevel.Success, originalLink);
                }
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
                Logger.LogWarning("link info: {Exception}", ex);
                return new LinkAndInfo(link, "(an error occurred)", FetchErrorLevel.TransientError, null);
            }
        }

        protected void PostLinkInfoToChannel(LinkAndInfo linkAndInfo, string channel)
        {
            PostLinkInfo(linkAndInfo, message => ConnectionManager.SendChannelMessage(channel, message));
        }

        protected void PostLinkInfo(LinkAndInfo linkAndInfo, Action<string> post)
        {
            string domainAnnotation = null;

            string[] hostNamePieces = linkAndInfo.Link.Host.ToLowerInvariant().Split('.');
            for (int i = 0; i < hostNamePieces.Length - 1; ++i)
            {
                string domainPiece = hostNamePieces.Skip(i).StringJoin(".");

                if (Config.DomainAnnotations.TryGetValue(domainPiece, out domainAnnotation))
                {
                    break;
                }
            }

            string domainAnnotationString = (domainAnnotation == null)
                ? ""
                : (" " + domainAnnotation);

            string linkString = linkAndInfo.Link.AbsoluteUri;
            string info = linkAndInfo.Info;
            if (linkAndInfo.ErrorLevel == FetchErrorLevel.Success && Config.FakeResponses.ContainsKey(linkString))
            {
                info = Config.FakeResponses[linkString];
            }

            string redirectedString = (linkAndInfo.OriginalLink != null)
                ? $"{ShortenLink(linkAndInfo.OriginalLink.AbsoluteUri)} -> "
                : "";

            post($"{redirectedString}{ShortenLink(linkString)} {(linkAndInfo.IsError ? ":!:" : "::")} {ShortenInfo(info)}{domainAnnotationString}");
        }

        protected bool TryCreateUriHeuristically(string word, out Uri uri)
        {
            uri = null;
            if (LinkDetector == null)
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

                var tlds = new HashSet<string>();
                using (var readStream = File.Open(tldListFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(readStream, StringUtil.Utf8NoBom))
                {
                    string line;
                    while ((line = reader.ReadLine()?.Trim()) != null)
                    {
                        if (line.StartsWith("#"))
                        {
                            continue;
                        }
                        tlds.Add(line.ToLowerInvariant());
                    }
                }

                LinkDetector = new HeuristicLinkDetector(tlds);
            }

            return LinkDetector.TryCreateUri(word, out uri);
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
                            wordChunks.Add(new TextMessageChunk(word));
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

        protected virtual string ShortenLink(string linkString)
        {
            return ShortenLink(linkString, Config.MaxLinkLength);
        }

        protected static string ShortenLink(string linkString, int maxLength /* = 256 */)
        {
            if (maxLength <= 0)
            {
                // don't shorten
                return linkString;
            }

            if (linkString.Length <= maxLength)
            {
                // good, good
                return linkString;
            }

            // try to remove some path elements first
            string[] bits = linkString.Split(new [] {'/'}, StringSplitOptions.None);

            if (bits.Length > 7)
            {
                // 0     12                   3    4  5    6        7         8  9
                // http://www.omg.example.com/path/to/some/resource/somewhere/or/other
                // keep first two and last two path segments intact

                var shortenedBits = new List<string>();
                shortenedBits.AddRange(bits.Take(5));
                shortenedBits.Add("[...]");
                shortenedBits.AddRange(bits.Skip(bits.Length - 2));

                string skipBitsPath = shortenedBits.StringJoin("/");
                if (skipBitsPath.Length <= maxLength)
                {
                    return skipBitsPath;
                }
            }

            // nope; we need to shorten a long path element
            List<int> indicesByLengthDesc = bits
                .Select((bit, i) => Tuple.Create(bit, i))
                .OrderByDescending(bi => bi.Item1.Length)
                .Select(bi => bi.Item2)
                .ToList();
            foreach (int longIndex in indicesByLengthDesc)
            {
                // either 16 characters or a third of the original string, whichever is shorter
                int preEllipsisLength = Math.Min(16, bits[longIndex].Length / 3);

                string shortenedString = string.Format(
                    "{0}[...]{1}",
                    bits[longIndex].Substring(0, preEllipsisLength),
                    bits[longIndex].Substring(bits[longIndex].Length - preEllipsisLength)
                );
                bits[longIndex] = shortenedString;

                // calculate final length
                string shortenedBitsPath = bits.StringJoin("/");
                if (shortenedBitsPath.Length <= maxLength)
                {
                    return shortenedBitsPath;
                }

                // shorten more
            }

            // still not short enough; just forcefully shorten the link
            string joinedUp = bits.StringJoin("/");
            return joinedUp.Substring(0, maxLength - 5) + "[...]";
        }

        protected virtual string ShortenInfo(string infoString)
        {
            return ShortenInfo(infoString, Config.MaxInfoLength);
        }

        protected static string ShortenInfo(string infoString, int maxLength /* = 512 */)
        {
            const string ellipsis = "[...]";

            if (maxLength <= 0)
            {
                return infoString;
            }

            if (infoString.Length <= maxLength)
            {
                return infoString;
            }

            // cut at words
            string[] words = infoString.Split(new[] {' '}, StringSplitOptions.None);
            for (int i = words.Length - 2; i > 0; --i)
            {
                string joinedEllipsized = words.Take(i).StringJoin(" ") + " " + ellipsis;
                if (joinedEllipsized.Length <= maxLength)
                {
                    return joinedEllipsized;
                }
            }

            // make a hard cut
            return infoString.Substring(0, maxLength - ellipsis.Length) + ellipsis;
        }
    }
}
