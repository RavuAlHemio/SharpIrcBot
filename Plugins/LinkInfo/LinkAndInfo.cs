using System;

namespace LinkInfo
{
    public class LinkAndInfo
    {
        public readonly Uri Link;
        public readonly string Info;
        public readonly bool TemporaryErrorOccurred;

        public bool HasInfo => Info != null;

        public LinkAndInfo(Uri link, string info = null, bool temporaryErrorOccurred = false)
        {
            Link = link;
            Info = info;
            TemporaryErrorOccurred = temporaryErrorOccurred;
        }

        public LinkAndInfo(Uri link, Tuple<bool, string> successAndInfo)
        {
            Link = link;
            Info = successAndInfo.Item2;
            TemporaryErrorOccurred = !successAndInfo.Item1;
        }
    }
}
