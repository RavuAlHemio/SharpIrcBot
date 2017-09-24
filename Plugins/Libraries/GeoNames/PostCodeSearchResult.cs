using System.Collections.Generic;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Libraries.GeoNames
{
    [JsonObject]
    public class PostCodeSearchResult
    {
        [JsonProperty("postalCodes")]
        public List<GeoName> PostCodeEntries { get; set; }
    }
}
