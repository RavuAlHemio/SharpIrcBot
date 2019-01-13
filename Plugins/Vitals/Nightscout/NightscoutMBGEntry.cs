using System;
using System.Globalization;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Vitals.Nightscout
{
    /// <remarks>
    /// MBG = Mean Blood Glucose
    /// </remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public class NightscoutMBGEntry
    {
        [JsonProperty("_id")]
        public string ID { get; set; }

        [JsonProperty("device")]
        public string Device { get; set; }

        [JsonProperty("dateString")]
        public string DateString
        {
            get => NightscoutUtils.TimestampToString(Timestamp);
            set { Timestamp = NightscoutUtils.ParseTimestamp(value); }
        }

        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("mbg")]
        public int MBG { get; set; }
        
        [JsonProperty("sysTime")]
        public string SystemTimeString
        {
            get => NightscoutUtils.TimestampToString(SystemTimestamp);
            set { SystemTimestamp = NightscoutUtils.ParseTimestamp(value); }
        }

        public DateTimeOffset SystemTimestamp { get; set; }
    }
}
