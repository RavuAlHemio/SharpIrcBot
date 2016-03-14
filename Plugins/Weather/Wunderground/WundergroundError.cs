using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Weather.Wunderground
{
    [JsonObject]
    public class WundergroundError
    {
        [NotNull, JsonProperty("type")]
        public string Type { get; set; }

        [NotNull, JsonProperty("description")]
        public string Description { get; set; }
    }
}
