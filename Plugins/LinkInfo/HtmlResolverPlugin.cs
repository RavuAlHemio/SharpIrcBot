using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.LinkInfo
{
    public class HtmlResolverPlugin : ILinkResolverPlugin
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<HtmlResolverPlugin>();
        public static readonly Regex WhiteSpaceRegex = new Regex("\\s+", RegexOptions.Compiled);

        public LinkInfoConfig LinkInfoConfig { get; set; }

        public HtmlResolverPlugin(JObject config, LinkInfoConfig linkInfoConfig)
        {
            LinkInfoConfig = linkInfoConfig;
        }

        public LinkAndInfo ResolveLink(LinkToResolve link)
        {
            if (link.ContentType.MediaType != "text/html" && link.ContentType.MediaType != "application/xhtml+xml")
            {
                return null;
            }

            // HTML? parse it and get the title
            var respStr = EncodingGuesser.GuessEncodingAndDecode(link.ResponseBytes, link.ContentType);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(respStr);
            var titleElement = htmlDoc.DocumentNode.SelectSingleNode(".//title");
            if (titleElement != null)
            {
                return link.ToResult(FetchErrorLevel.Success, FoldWhitespace(HtmlEntity.DeEntitize(titleElement.InnerText)).Trim());
            }
            var h1Element = htmlDoc.DocumentNode.SelectSingleNode(".//h1");
            if (h1Element != null)
            {
                return link.ToResult(FetchErrorLevel.Success, FoldWhitespace(HtmlEntity.DeEntitize(h1Element.InnerText)).Trim());
            }
            return link.ToResult(FetchErrorLevel.Success, "(HTML without a title O_o)");
        }

        public static string FoldWhitespace(string str)
        {
            return WhiteSpaceRegex.Replace(str, " ");
        }
    }
}
