using System;
using JetBrains.Annotations;

namespace SharpIrcBot.Plugins.Counters.ORM
{
    public class CounterEntry
    {
        public long ID { get; set; }

        public string Command { get; set; }

        public string Channel { get; set; }

        public DateTimeOffset HappenedTimestamp { get; set; }

        public DateTimeOffset CountedTimestamp { get; set; }

        public string PerpNickname { get; set; }

        [CanBeNull]
        public string PerpUsername { get; set; }

        public string CounterNickname { get; set; }

        [CanBeNull]
        public string CounterUsername { get; set; }

        public string Message { get; set; }
    }
}
