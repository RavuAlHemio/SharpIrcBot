using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Weather.Wunderground
{
    [JsonObject]
    public class WundergroundForecast
    {
        [CanBeNull, JsonProperty("simpleforecast")]
        public WundergroundSimpleForecast Simple { get; set; }
    }
}
