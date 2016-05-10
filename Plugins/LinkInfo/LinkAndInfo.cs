using System;
using JetBrains.Annotations;

namespace LinkInfo
{
    public class LinkAndInfo
    {
        [NotNull]
        public Uri Link { get; }
        [CanBeNull]
        public Uri OriginalLink { get; }
        [CanBeNull]
        public string Info { get; }
        public FetchErrorLevel ErrorLevel { get; }
        public bool IsError => ErrorLevel != FetchErrorLevel.Success;
        public bool ShouldRefetch
            => ErrorLevel == FetchErrorLevel.Unfetched || ErrorLevel == FetchErrorLevel.TransientError;

        public bool HasInfo => Info != null;

        public LinkAndInfo([NotNull] Uri link, [CanBeNull] string info, FetchErrorLevel errorLevel, [CanBeNull] Uri originalLink = null)
        {
            Link = link;
            OriginalLink = originalLink;
            Info = info;
            ErrorLevel = errorLevel;
        }

        public static LinkAndInfo CreateUnfetched(Uri link)
        {
            return new LinkAndInfo(link, info: null, errorLevel: FetchErrorLevel.Unfetched, originalLink: null);
        }
    }
}
