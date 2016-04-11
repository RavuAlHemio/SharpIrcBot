using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SharpIrcBot.Events
{
    public class NickMappingEventArgs : EventArgs
    {
        [NotNull]
        public string Nickname { get; private set; }
        [NotNull, ItemNotNull]
        public IList<string> MapsTo { get; private set; }

        public NickMappingEventArgs(string nickname)
            : base()
        {
            Nickname = nickname;
            MapsTo = new List<string>();
        }
    }
}
