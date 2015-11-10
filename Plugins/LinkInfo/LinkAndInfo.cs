using System;

namespace LinkInfo
{
    public class LinkAndInfo
    {
        public Uri Link { get; }
        public string Info { get; }
        public FetchErrorLevel ErrorLevel { get; }
        public bool IsError => ErrorLevel != FetchErrorLevel.Success;
        public bool ShouldRefetch
            => ErrorLevel == FetchErrorLevel.Unfetched || ErrorLevel == FetchErrorLevel.TransientError;

        public bool HasInfo => Info != null;

        public LinkAndInfo(Uri link, string info, FetchErrorLevel errorLevel)
        {
            Link = link;
            Info = info;
            ErrorLevel = errorLevel;
        }

        public static LinkAndInfo CreateUnfetched(Uri link)
        {
            return new LinkAndInfo(link, info: null, errorLevel: FetchErrorLevel.Unfetched);
        }
    }
}
