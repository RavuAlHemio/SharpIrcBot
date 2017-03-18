using System;
using JetBrains.Annotations;

namespace SharpIrcBot.Plugins.Thanks.ORM
{
    public class ThanksEntry
    {
        public long ID { get; set; }

        public DateTime Timestamp { get; set; }

        public string ThankerLowercase { get; set; }

        public string ThankeeLowercase { get; set; }

        public string Channel { get; set; }

        [CanBeNull]
        public string Reason { get; set; }

        public bool Deleted { get; set; }
    }
}
