using System.Collections.Generic;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OWMWeatherState
    {
        [JsonProperty("weather")]
        public List<OWMWeather> Weathers { get; set; }

        [JsonProperty("main")]
        public OWMMain Main { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("dt")]
        public long UnixTimestamp { get; set; }

        public OWMWeatherState()
        {
            Weathers = new List<OWMWeather>();
        }
    }
}
