using System;
using JetBrains.Annotations;

namespace SharpIrcBot.Plugins.LinkInfo
{
    public class LinkToResolve
    {
        [NotNull]
        public Uri Link { get; set; }

        [CanBeNull]
        public Uri OriginalLink { get; set; }

        [NotNull]
        public Uri OriginalLinkOrLink => OriginalLink ?? Link;

        [NotNull]
        public byte[] ResponseBytes { get; set; }

        [NotNull]
        public string ContentType { get; set; }

        public LinkToResolve([NotNull] Uri link, [CanBeNull] Uri originalLink, [NotNull] byte[] responseBytes,
                [NotNull] string contentType)
        {
            Link = link;
            OriginalLink = originalLink;
            ResponseBytes = responseBytes;
            ContentType = contentType;
        }

        public LinkAndInfo ToResult(FetchErrorLevel errorLevel, string info)
        {
            return new LinkAndInfo(Link, info, errorLevel, OriginalLink);
        }
    }
}
