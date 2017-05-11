using System;

namespace SharpIrcBot.Plugins.Demoderation.ORM
{
    public class Abuse
    {
        public long ID { get; set; }
        public long BanID { get; set; }
        public Ban Ban { get; set; }
        public string OpNickname { get; set; }
        public string OpUsername { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset BanUntil { get; set; }
        public DateTimeOffset LockUntil { get; set; }
        public bool Lifted { get; set; }
    }
}
