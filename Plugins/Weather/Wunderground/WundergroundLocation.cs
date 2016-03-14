using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Weather.Wunderground
{
    [JsonObject]
    public class WundergroundLocation
    {
        [NotNull, JsonProperty("full")]
        public string FullName { get; set; }
    }
}
