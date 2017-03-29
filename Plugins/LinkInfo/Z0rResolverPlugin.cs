using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.LinkInfo
{
    public class Z0rResolverPlugin : ILinkResolverPlugin
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<Z0rResolverPlugin>();

        public static readonly Regex Z0rUrlPattern = new Regex("^http://z0r.de/(?<id>[0-9]+)$", RegexOptions.Compiled);
        public static readonly Regex PageHrefPattern = new Regex("^\\./Seite(?<page>[0-9]+)\\.html$", RegexOptions.Compiled);

        public static readonly Uri Z0rIndexHomepageUri = new Uri("http://index.z0r.de/");
        public const string Z0rIndexUriFormat = "http://index.z0r.de/Seite{0}.html";

        public LinkInfoConfig LinkInfoConfig { get; set; }

        protected Dictionary<long, Z0rEntry> EntryCache { get; }
        protected long? MaxPage { get; set; }

        public Z0rResolverPlugin(JObject config, LinkInfoConfig linkInfoConfig)
        {
            LinkInfoConfig = linkInfoConfig;
            EntryCache = new Dictionary<long, Z0rEntry>();
            MaxPage = null;
        }

        public LinkAndInfo ResolveLink(LinkToResolve link)
        {
            string absoluteUri = link.Link.AbsoluteUri;
            Match z0rMatch = Z0rUrlPattern.Match(absoluteUri);
            if (!z0rMatch.Success)
            {
                // can't handle this
                return null;
            }

            // obtain the ID
            long z0rID;
            if (!long.TryParse(z0rMatch.Groups["id"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out z0rID))
            {
                // unparseable ID, probably too many digits
                return null;
            }

            Z0rEntry entry;
            if (EntryCache.TryGetValue(z0rID, out entry))
            {
                // fast-path
                return link.ToResult(FetchErrorLevel.Success, FormatEntry(entry));
            }

            Z0rRange range = RangeForID(z0rID);

            if (!MaxPage.HasValue)
            {
                MaxPage = ObtainMaxPageValue();
            }

            if (!MaxPage.HasValue)
            {
                // bad
                return link.ToResult(
                    FetchErrorLevel.TransientError,
                    string.Format(CultureInfo.InvariantCulture, "z0r #{0}; fetching index page list failed", z0rID)
                );
            }

            if (range.Page > MaxPage)
            {
                // the index does not contain this page
                entry = new Z0rEntry(z0rID, null, null, null, null);
                return link.ToResult(FetchErrorLevel.Success, FormatEntry(entry));
            }

            LoadFromPage(range.Page);

            if (EntryCache.TryGetValue(z0rID, out entry))
            {
                return link.ToResult(FetchErrorLevel.Success, FormatEntry(entry));
            }

            return link.ToResult(
                FetchErrorLevel.TransientError,
                string.Format(CultureInfo.InvariantCulture, "z0r #{0}; fetching failed", z0rID)
            );
        }

        static Z0rRange RangeForID(long z0rID)
        {
            long first = ((z0rID / 100) * 100);
            long last = first + 99;
            long page = (z0rID / 100) + 1;
            return new Z0rRange(first, last, page);
        }

        static string FormatEntry(Z0rEntry entry)
        {
            var ret = new StringBuilder();
            ret.AppendFormat(CultureInfo.InvariantCulture, "z0r #{0}", entry.Z0rID);

            if (entry.Artist != null || entry.Song != null || entry.Image != null)
            {
                ret.Append(" (");

                if (entry.Artist != null && entry.Song != null)
                {
                    ret.AppendFormat("{0} - {1}", entry.Artist, entry.Song);
                }
                else if (entry.Song != null)
                {
                    ret.AppendFormat("{0}", entry.Song);
                }
                else if (entry.Artist != null)
                {
                    ret.AppendFormat("{0} - ?", entry.Artist);
                }
                else
                {
                    ret.Append("?");
                }

                ret.Append(" // ");
                ret.Append(entry.Image != null ? entry.Image : "?");
                ret.Append(")");
            }

            return ret.ToString();
        }

        long? ObtainMaxPageValue()
        {
            // obtain the index homepage
            byte[] indexHomepageBytes;
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, Z0rIndexHomepageUri))
            {
                client.Timeout = TimeSpan.FromSeconds(LinkInfoConfig.TimeoutSeconds);
                request.Headers.UserAgent.TryParseAdd(LinkInfoConfig.FakeUserAgent);

                using (var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).SyncWait())
                {
                    indexHomepageBytes = response.Content.ReadAsByteArrayAsync().SyncWait();
                }
            }
            string indexHomepageString = EncodingGuesser.GuessEncodingAndDecode(indexHomepageBytes, null);

            long? currentMaxPage = null;

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(indexHomepageString);
            IEnumerable<Match> foundLinkMatches = htmlDoc.DocumentNode
                .SelectNodes(".//a")
                .OfType<HtmlNode>()
                .Select(n => PageHrefPattern.Match(n.GetAttributeValue("href", "")))
                .Where(m => m.Success);
            foreach (Match foundLinkMatch in foundLinkMatches)
            {
                long page;
                if (!long.TryParse(foundLinkMatch.Groups["page"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out page))
                {
                    continue;
                }

                if (!currentMaxPage.HasValue || currentMaxPage.Value < page)
                {
                    currentMaxPage = page;
                }
            }
            return currentMaxPage;
        }

        void LoadFromPage(long page)
        {
            var pageUri = new Uri(string.Format(CultureInfo.InvariantCulture, Z0rIndexUriFormat, page));

            byte[] indexPageBytes;
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, pageUri))
            {
                client.Timeout = TimeSpan.FromSeconds(LinkInfoConfig.TimeoutSeconds);
                request.Headers.UserAgent.TryParseAdd(LinkInfoConfig.FakeUserAgent);

                using (var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).SyncWait())
                {
                    indexPageBytes = response.Content.ReadAsByteArrayAsync().SyncWait();
                }
            }
            string indexPageString = EncodingGuesser.GuessEncodingAndDecode(indexPageBytes, null);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(indexPageString);

            HtmlNode indexTable = htmlDoc.GetElementbyId("zebra");
            IEnumerable<HtmlNode> foundRows = indexTable
                .SelectNodes(".//tr")
                .OfType<HtmlNode>();
            foreach (HtmlNode foundRow in foundRows)
            {
                List<HtmlNode> cells = foundRow
                    .ChildNodes
                    .OfType<HtmlNode>()
                    .Where(n => n.Name == "th")
                    .ToList();
                if (cells.Count != 5)
                {
                    continue;
                }

                string idString = TrimmedInnerTextOrNull(cells[0]);

                long id;
                if (!long.TryParse(idString, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                {
                    continue;
                }

                string artist = TrimmedInnerTextOrNull(cells[1]);
                string song = TrimmedInnerTextOrNull(cells[2]);
                string image = TrimmedInnerTextOrNull(cells[3]);
                string tag = TrimmedInnerTextOrNull(cells[4]);

                EntryCache[id] = new Z0rEntry(id, artist, song, image, tag);
            }
        }

        static string TrimmedInnerTextOrNull(HtmlNode node)
        {
            if (node == null)
            {
                return null;
            }

            string trimmed = node.InnerText.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }
            return trimmed;
        }

        public struct Z0rRange : IEquatable<Z0rRange>
        {
            public long First { get; }
            public long Last { get; }
            public long Page { get; }

            public Z0rRange(long first, long last, long page)
            {
                First = first;
                Last = last;
                Page = page;
            }

            public bool Equals(Z0rRange other)
            {
                return
                    this.First == other.First &&
                    this.Last == other.Last &&
                    this.Page == other.Page;
            }

            public override bool Equals(object other)
            {
                if (other == null)
                {
                    return false;
                }

                if (this.GetType() != other.GetType())
                {
                    return false;
                }

                return this.Equals((Z0rRange)other);
            }

            public override int GetHashCode()
            {
                return unchecked(
                    2 * First.GetHashCode() +
                    7 * Last.GetHashCode() +
                    11 * Page.GetHashCode()
                );
            }

            public override string ToString()
            {
                return $"Z0rRange({First}..{Last}, p{Page})";
            }

            public static bool operator ==(Z0rRange a, Z0rRange b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(Z0rRange a, Z0rRange b)
            {
                return !(a == b);
            }
        }

        public class Z0rEntry
        {
            public long Z0rID { get; }
            public string Artist { get; }
            public string Song { get; }
            public string Image { get; }
            public string Tag { get; }

            public Z0rEntry(long z0rID, string artist, string song, string image, string tag)
            {
                Z0rID = z0rID;
                Artist = artist;
                Song = song;
                Image = image;
                Tag = tag;
            }
        }
    }
}
