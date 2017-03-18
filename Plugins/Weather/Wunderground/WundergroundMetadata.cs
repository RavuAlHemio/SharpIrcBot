using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.Wunderground
{
    [JsonObject]
    public class WundergroundMetadata
    {
        [CanBeNull, JsonProperty("error")]
        public WundergroundError Error { get; set; }

        [CanBeNull, JsonProperty("results")]
        public List<WundergroundLocationMatch> LocationMatches { get; set; }
    }
}
