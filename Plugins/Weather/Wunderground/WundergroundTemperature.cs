using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.Wunderground
{
    [JsonObject]
    public class WundergroundTemperature
    {
        [JsonProperty("fahrenheit")]
        public decimal? Fahrenheit { get; set; }

        [JsonProperty("celsius")]
        public decimal? Celsius { get; set; }
    }
}
