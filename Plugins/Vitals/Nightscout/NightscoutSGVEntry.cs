using System;
using System.Globalization;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Vitals.Nightscout
{
    /// <remarks>
    /// SGV = Sensor Glucose Value
    /// </remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public class NightscoutSGVEntry
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

        [JsonProperty("sgv")]
        public decimal SGV { get; set; }

        [JsonProperty("delta")]
        public decimal Delta { get; set; }

        [JsonProperty("direction")]
        public string Direction { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("filtered")]
        public long Filtered { get; set; }

        [JsonProperty("unfiltered")]
        public long Unfiltered { get; set; }

        /// <remarks>
        /// RSSI = Received Signal Strength Indication
        /// </remarks>
        [JsonProperty("rssi")]
        public int RSSI { get; set; }

        [JsonProperty("noise")]
        public int Noise { get; set; }

        
        [JsonProperty("sysTime")]
        public string SystemTimeString
        {
            get => NightscoutUtils.TimestampToString(SystemTimestamp);
            set { SystemTimestamp = NightscoutUtils.ParseTimestamp(value); }
        }

        public DateTimeOffset SystemTimestamp { get; set; }
    }
}
