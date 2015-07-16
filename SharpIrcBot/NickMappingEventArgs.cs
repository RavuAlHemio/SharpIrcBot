using System;
using System.Collections.Generic;

namespace SharpIrcBot
{
    public class NickMappingEventArgs : EventArgs
    {
        public string Nickname { get; private set; }
        public IList<string> MapsTo { get; private set; }

        public NickMappingEventArgs(string nickname)
            : base()
        {
            Nickname = nickname;
            MapsTo = new List<string>();
        }
    }
}
