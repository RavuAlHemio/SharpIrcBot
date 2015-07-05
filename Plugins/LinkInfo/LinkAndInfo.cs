using System;

namespace LinkInfo
{
    class LinkAndInfo
    {
        public readonly Uri Link;
        public readonly string Info;

        public LinkAndInfo(Uri link, string info)
        {
            Link = link;
            Info = info;
        }
    }
}
