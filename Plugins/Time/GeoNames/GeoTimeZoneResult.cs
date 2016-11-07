using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Time.GeoNames
{
    [JsonObject]
    public class GeoTimeZoneResult
    {
        const string DateFormat = "yyyy-MM-dd HH:mm";

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("countryName")]
        public string CountryName { get; set; }

        [JsonProperty("dstOffset")]
        public int DSTOffset { get; set; }

        [JsonProperty("gmtOffset")]
        public int GMTOffset { get; set; }

        [JsonProperty("lat")]
        public decimal Latitude { get; set; }

        [JsonProperty("lng")]
        public decimal Longitude { get; set; }

        [JsonProperty("rawOffset")]
        public int RawOffset { get; set; }

        [JsonProperty("sunrise")]
        public string SunriseString
        {
            get
            {
                return Sunrise.ToString(DateFormat, CultureInfo.InvariantCulture);
            }

            set
            {
                Sunrise = DateTime.ParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            }
        }

        [JsonProperty("sunset")]
        public string SunsetString
        {
            get
            {
                return Sunset.ToString(DateFormat, CultureInfo.InvariantCulture);
            }

            set
            {
                Sunset = DateTime.ParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            }
        }

        [JsonProperty("time")]
        public string TimeString
        {
            get
            {
                return Time.ToString(DateFormat, CultureInfo.InvariantCulture);
            }

            set
            {
                Time = DateTime.ParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            }
        }

        [JsonProperty("timezoneId")]
        public string TimezoneID { get; set; }

        [JsonIgnore]
        public DateTime Sunrise { get; set; }

        [JsonIgnore]
        public DateTime Sunset { get; set; }

        [JsonIgnore]
        public DateTime Time { get; set; }
    }
}
