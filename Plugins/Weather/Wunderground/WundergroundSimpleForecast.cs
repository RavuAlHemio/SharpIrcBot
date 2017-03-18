using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.Wunderground
{
    [JsonObject]
    public class WundergroundSimpleForecast
    {
        [CanBeNull, ItemNotNull, JsonProperty("forecastday")]
        public List<WundergroundSimpleForecastDay> Days { get; set; }
    }
}
