using System.Collections.Generic;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Time.GeoNames
{
    [JsonObject]
    public class GeoSearchResult
    {
        [JsonProperty("totalResultsCount")]
        public long TotalResultsCount { get; set; }

        [JsonProperty("geonames")]
        public List<GeoName> GeoNames { get; set; }
    }
}
