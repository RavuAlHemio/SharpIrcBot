using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.LinkInfo
{
    public class ReverseGoogleImageResolverPlugin : ILinkResolverPlugin
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<ReverseGoogleImageResolverPlugin>();

        [JsonObject(MemberSerialization.OptOut)]
        public class RGIRPConfig
        {
            public double ImageInfoTimeoutSeconds { get; set; } = 1.0;

            [CanBeNull]
            public string DumpImageResultsFileName { get; set; } = null;
        }

        public const string GoogleHomepageUrlPattern = "https://{0}/";
        public const string GoogleImageSearchUrlPattern = "https://{0}/imghp?hl=en&tab=wi";
        public const string GoogleImageSearchByImageUrlPattern = "https://{0}/searchbyimage?hl=en&image_url={1}";
        public static readonly ImmutableDictionary<string, string> DetectedMimeTypes =
            ImmutableDictionary.CreateBuilder<string, string>()
            .Adding("image/png", "PNG image")
            .Adding("image/jpeg", "JPEG image")
            .Adding("image/gif", "GIF image")
            .ToImmutableDictionary()
        ;

        public RGIRPConfig Config { get; set; }

        public LinkInfoConfig LinkInfoConfig { get; set; }

        public ReverseGoogleImageResolverPlugin(JObject config, LinkInfoConfig linkInfoConfig)
        {
            Config = new RGIRPConfig();
            JsonSerializer.CreateDefault().Populate(config.CreateReader(), Config);
            LinkInfoConfig = linkInfoConfig;
        }

        public LinkAndInfo ResolveLink(LinkToResolve link)
        {
            if (link.ContentType?.MediaType == null)
            {
                return null;
            }

            string typeDescription;
            if (!DetectedMimeTypes.TryGetValue(link.ContentType.MediaType, out typeDescription))
            {
                // you're not my type
                return null;
            }

            string description = ResolveLinkText(link, typeDescription);
            return link.ToResult(FetchErrorLevel.Success, description);
        }

        string ResolveLinkText(LinkToResolve link, string typeDescription)
        {
            try
            {
                var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(Config.ImageInfoTimeoutSeconds)
                };

                var googleImageSearchUrl = new Uri(string.Format(GoogleImageSearchUrlPattern, LinkInfoConfig.GoogleDomain));

                // alibi-visit the image search page to get the cookies
                using (var request = new HttpRequestMessage(HttpMethod.Get, googleImageSearchUrl))
                {
                    request.Headers.UserAgent.TryParseAdd(LinkInfoConfig.FakeUserAgent);
                    request.Headers.Referrer = new Uri(string.Format(GoogleHomepageUrlPattern, LinkInfoConfig.GoogleDomain));

                    using (var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).SyncWait())
                    {
                        response.Content.ReadAsByteArrayAsync().SyncWait();
                    }
                }

                // fetch the actual info
                var searchUrl = new Uri(string.Format(
                    GoogleImageSearchByImageUrlPattern,
                    LinkInfoConfig.GoogleDomain,
                    link.Link.AbsoluteUri
                ));
                byte[] responseBytes;
                using (var request = new HttpRequestMessage(HttpMethod.Get, searchUrl))
                {
                    request.Headers.UserAgent.TryParseAdd(LinkInfoConfig.FakeUserAgent);
                    request.Headers.Referrer = googleImageSearchUrl;

                    using (var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).SyncWait())
                    {
                        responseBytes = response.Content.ReadAsByteArrayAsync().SyncWait();
                    }
                }
                var parseMe = EncodingGuesser.GuessEncodingAndDecode(responseBytes, null);

                if (Config.DumpImageResultsFileName != null)
                {
                    using (var dumpy = File.Open(Path.Combine(SharpIrcBotUtil.AppDirectory, Config.DumpImageResultsFileName), FileMode.Create, FileAccess.Write))
                    {
                        dumpy.Write(responseBytes, 0, responseBytes.Length);
                    }
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(parseMe);
                IEnumerable<HtmlNode> foundHubs = htmlDoc.DocumentNode
                    .SelectNodes(".//*")
                    .OfType<HtmlNode>()
                    .Where(n => n.GetAttributeValue("class", "").Split(' ').Contains("_hUb"));
                foreach (HtmlNode foundHub in foundHubs)
                {
                    IEnumerable<HtmlNode> foundGubs = foundHub
                        .SelectNodes(".//*")
                        .OfType<HtmlNode>()
                        .Where(n => n.GetAttributeValue("class", "").Split(' ').Contains("_gUb"));
                    foreach (HtmlNode hint in foundGubs)
                    {
                        return string.Format("{0} ({1})", typeDescription, HtmlEntity.DeEntitize(hint.InnerText));
                    }
                }
                return typeDescription;
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                // timed out
                return typeDescription;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("image info: {Exception}", ex);
                return typeDescription;
            }
        }
    }
}
