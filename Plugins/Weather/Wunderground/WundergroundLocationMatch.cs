using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Weather.Wunderground
{
    [JsonObject]
    public class WundergroundLocationMatch
    {
        [NotNull, JsonProperty("name")]
        public string Name { get; set; }

        [NotNull, JsonProperty("city")]
        public string City { get; set; }

        [NotNull, JsonProperty("state")]
        public string State { get; set; }

        [NotNull, JsonProperty("country_name")]
        public string Country { get; set; }

        [NotNull, JsonProperty("country_iso3166")]
        public string CountryCode { get; set; }

        [NotNull, JsonProperty("zmw")]
        public string WundergroundLocationID { get; set; }
    }
}
