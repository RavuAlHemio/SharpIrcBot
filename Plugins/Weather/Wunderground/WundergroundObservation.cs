using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.Wunderground
{
    [JsonObject]
    public class WundergroundObservation
    {
        [CanBeNull, JsonProperty("temp_c")]
        public decimal? Temperature { get; set; }

        [NotNull, JsonProperty("relative_humidity")]
        public string Humidity { get; set; }

        [NotNull, JsonProperty("weather")]
        public string WeatherDescription { get; set; }

        [CanBeNull, JsonProperty("feelslike_c")]
        public decimal? FeelsLikeTemperature { get; set; }

        [NotNull, JsonProperty("display_location")]
        public WundergroundLocation DisplayLocation { get; set; }

        [CanBeNull, JsonProperty("observation_epoch")]
        public long? LastUpdateUnixTimestamp { get; set; }
    }
}
