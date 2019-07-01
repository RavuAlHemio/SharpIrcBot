using System;

namespace SharpIrcBot.Plugins.ClientVersion.ORM
{
    public class VersionEntry
    {
        public long ID { get; set; }

        public string Nickname { get; set; }

        public string VersionInfo { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
