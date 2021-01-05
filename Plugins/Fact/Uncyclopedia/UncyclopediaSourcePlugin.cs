using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Fact.Uncyclopedia
{
    public class UncyclopediaSourcePlugin : IFactSourcePlugin
    {
        [JsonObject(MemberSerialization.OptOut)]
        public class Config
        {
            [JsonIgnore] public Uri SourceUri { get; set; }
            [JsonIgnore] public Uri WikitextLinkBaseUri { get; set; }
            [JsonIgnore] public Regex StartStripRegex { get; set; }
            [JsonIgnore] public Regex EndStripRegex { get; set; }

            public string PrefixText { get; set; }

            [JsonProperty("SourceUri")]
            public string SourceUriString
            {
                get { return SourceUri.ToString(); }
                set { SourceUri = new Uri(value); }
            }

            [JsonProperty("WikitextLinkBaseUri")]
            public string WikitextLinkBaseUriString
            {
                get { return WikitextLinkBaseUri.ToString(); }
                set { WikitextLinkBaseUri = new Uri(value); }
            }

            [JsonProperty("StartStripRegex")]
            public string StartStripRegexString
            {
                get { return StartStripRegex.ToString(); }
                set { StartStripRegex = new Regex(value); }
            }

            [JsonProperty("EndStripRegex")]
            public string EndStripRegexString
            {
                get { return EndStripRegex.ToString(); }
                set { EndStripRegex = new Regex(value); }
            }
        }

        protected List<string> Facts { get; set; }

        public UncyclopediaSourcePlugin(JObject configJson)
        {
            Facts = new List<string>();

            var config = new Config();
            var ser = new JsonSerializer();
            ser.Populate(configJson.CreateReader(), config);

            // obtain data from the URI and parse it as JSON
            string data;
            using (var client = new HttpClient())
            {
                data = client.GetStringAsync(config.SourceUri)
                    .SyncWait();
            }
            var pageData = JObject.Parse(data);

            // obtain the page text
            var pageWikitext = (string)pageData["query"]["pages"][0]["revisions"][0]["slots"]["main"]["content"];

            // split up the facts
            var facts = new List<string>();
            int curIndex = 0;
            for (;;)
            {
                Match startMatch = config.StartStripRegex.Match(pageWikitext, curIndex);
                if (!startMatch.Success)
                {
                    break;
                }

                Match endMatch = config.EndStripRegex.Match(pageWikitext, curIndex + startMatch.Length);
                if (!endMatch.Success)
                {
                    break;
                }

                string factWikitext = pageWikitext.Substring(
                    startMatch.Index + startMatch.Length,
                    endMatch.Index - (startMatch.Index + startMatch.Length)
                );

                var (strippedText, links) = ExtractWikitextLinks(factWikitext, config.WikitextLinkBaseUri);

                var factBits = new StringBuilder();
                if (config.PrefixText != null)
                {
                    factBits.Append(config.PrefixText);
                }
                factBits.Append(strippedText);
                foreach (Uri link in links)
                {
                    factBits.Append(' ');
                    factBits.Append(link.ToString());
                }
                facts.Add(factBits.ToString());

                curIndex = endMatch.Index + endMatch.Length + 1;
            }

            Facts = facts;
        }

        public static (string, List<Uri>) DefaultExtractWikitextLinks(string wikitext, Uri baseUri)
        {
            var strippedWikitext = new StringBuilder();
            var extractedLinks = new List<Uri>();
            int curIndex = 0;
            for (;;)
            {
                int openingBracketIndex = wikitext.IndexOf("[[", curIndex);
                if (openingBracketIndex == -1)
                {
                    break;
                }

                int closingBracketIndex = wikitext.IndexOf("]]", openingBracketIndex + 2);
                if (closingBracketIndex == -1)
                {
                    break;
                }

                int pipeIndex = wikitext.IndexOf('|', openingBracketIndex);
                if (pipeIndex > closingBracketIndex)
                {
                    pipeIndex = -1;
                }

                string uriText, bodyText;
                if (pipeIndex == -1)
                {
                    // both are the same
                    uriText = wikitext.Substring(openingBracketIndex + 2, closingBracketIndex - (openingBracketIndex + 2));
                    bodyText = uriText;
                }
                else
                {
                    // split by pipe
                    uriText = wikitext.Substring(openingBracketIndex + 2, pipeIndex - (openingBracketIndex + 2));
                    bodyText = wikitext.Substring(pipeIndex + 1, closingBracketIndex - (pipeIndex + 1));
                    if (bodyText.Length == 0)
                    {
                        // URI text with prefixes stripped
                        int lastColonIndex = uriText.LastIndexOf(':');
                        // works correctly even if lastColonIndex == -1:
                        bodyText = uriText.Substring(lastColonIndex + 1);
                    }
                }

                // normalize the URI text
                if (uriText.Length > 0)
                {
                    var uriTextBuilder = new StringBuilder(uriText);

                    // remove leading colons (:File:Test.jpeg => File:Test.jpeg)
                    while (uriTextBuilder.Length > 0 && uriTextBuilder[0] == ':')
                    {
                        uriTextBuilder.Remove(0, 1);
                    }

                    // replace spaces with underscores
                    uriTextBuilder.Replace(" ", "_");

                    // capitalize first letter
                    if (uriTextBuilder.Length > 0)
                    {
                        uriTextBuilder[0] = char.ToUpperInvariant(uriTextBuilder[0]);
                    }

                    // URL-encode
                    uriText = UriUtil.UrlEncode(uriTextBuilder.ToString(), Encoding.UTF8);
                }

                // collect the URI
                var linkUri = new Uri(baseUri, uriText);

                // collect it all
                strippedWikitext.Append(wikitext.Substring(curIndex, openingBracketIndex - curIndex));
                strippedWikitext.Append(bodyText);
                extractedLinks.Add(linkUri);

                curIndex = closingBracketIndex + 2;
            }
            strippedWikitext.Append(wikitext.Substring(curIndex));

            return (strippedWikitext.ToString(), extractedLinks);
        }

        protected virtual (string, List<Uri>) ExtractWikitextLinks(string wikitext, Uri baseUri)
        {
            return DefaultExtractWikitextLinks(wikitext, baseUri);
        }

        public string GetRandomFact([NotNull] Random rng)
        {
            int index = rng.Next(Facts.Count);
            return Facts[index];
        }
    }
}
