using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OWMWeather
    {
        [JsonProperty("id")]
        public long ID { get; set; }

        [JsonProperty("main")]
        public string Main { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }
}
