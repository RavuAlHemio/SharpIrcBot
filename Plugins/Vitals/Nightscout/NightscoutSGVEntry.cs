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
        public const string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffzzz";

        [JsonProperty("_id")]
        public string ID { get; set; }

        [JsonProperty("device")]
        public string Device { get; set; }

        [JsonProperty("dateString")]
        public string DateString
        {
            get => TimestampToString(Timestamp);
            set { Timestamp = ParseTimestamp(value); }
        }

        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("sgv")]
        public int SGV { get; set; }

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
            get => TimestampToString(SystemTimestamp);
            set { SystemTimestamp = ParseTimestamp(value); }
        }

        public DateTimeOffset SystemTimestamp { get; set; }

        private static DateTimeOffset ParseTimestamp(string text)
        {
            // add colon to timezone
            text = text.Insert(text.Length - 2, ":");

            // standard parse
            return DateTimeOffset.ParseExact(text, DateFormat, CultureInfo.InvariantCulture);
        }

        private static string TimestampToString(DateTimeOffset timestamp)
        {
            string text = timestamp.ToString(DateFormat, CultureInfo.InvariantCulture);

            // remove colon from timezone
            return text.Remove(text.Length - 3, 1);
        }
    }
}
