using System.Collections.Generic;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OWMForecast
    {
        [JsonProperty("list")]
        public List<OWMWeatherState> WeatherStates { get; set; }

        public OWMForecast()
        {
            WeatherStates = new List<OWMWeatherState>();
        }
    }
}
