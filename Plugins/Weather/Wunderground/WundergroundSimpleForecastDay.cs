using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.Wunderground
{
    [JsonObject]
    public class WundergroundSimpleForecastDay
    {
        [NotNull, JsonProperty("date")]
        public WundergroundDate Date { get; set; }

        [NotNull, JsonProperty("high")]
        public WundergroundTemperature HighTemperature { get; set; }

        [NotNull, JsonProperty("low")]
        public WundergroundTemperature LowTemperature { get; set; }

        [CanBeNull, JsonProperty("conditions")]
        public string Conditions { get; set; }
    }
}
