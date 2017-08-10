using System.Collections.Generic;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Libraries.GeoNames
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
