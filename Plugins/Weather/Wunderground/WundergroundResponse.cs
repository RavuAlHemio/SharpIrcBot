using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Weather.Wunderground
{
    [JsonObject]
    public class WundergroundResponse
    {
        [NotNull, JsonProperty("response")]
        public WundergroundMetadata Metadata { get; set; }

        [CanBeNull, JsonProperty("current_observation")]
        public WundergroundObservation CurrentWeather { get; set; }

        [CanBeNull, JsonProperty("forecast")]
        public WundergroundForecast Forecast { get; set; }
    }
}
