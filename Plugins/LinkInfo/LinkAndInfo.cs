using System;

namespace LinkInfo
{
    public class LinkAndInfo
    {
        public readonly Uri Link;
        public readonly string Info;

        public bool HasInfo
        {
            get
            {
                return Info != null;
            }
        }

        public LinkAndInfo(Uri link, string info = null)
        {
            Link = link;
            Info = info;
        }
    }
}
