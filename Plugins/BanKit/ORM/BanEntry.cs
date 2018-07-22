using System;

namespace SharpIrcBot.Plugins.BanKit.ORM
{
    public class BanEntry
    {
        public long ID { get; set; }

        public string BannedNick { get; set; }

        public string BannedMask { get; set; }

        public string BannerNick { get; set; }

        public string Channel { get; set; }

        public DateTimeOffset TimestampBanStart { get; set; }

        public DateTimeOffset TimestampBanEnd { get; set; }

        public string Reason { get; set; }

        public bool Lifted { get; set; }
    }
}
