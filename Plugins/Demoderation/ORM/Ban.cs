using System;

namespace SharpIrcBot.Plugins.Demoderation.ORM
{
    public class Ban
    {
        public long ID { get; set; }
        public long CriterionID { get; set; }
        public Criterion Criterion { get; set; }
        public string Channel { get; set; }
        public string OffenderNickname { get; set; }
        public string OffenderUsername { get; set; }
        public string BannerNickname { get; set; }
        public string BannerUsername { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset BanUntil { get; set; }
        public bool Lifted { get; set; }
    }
}
