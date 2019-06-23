using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OWMMain
    {
        [JsonProperty("temp")]
        public decimal TemperatureKelvin { get; set; }

        [JsonProperty("pressure")]
        public decimal PressureHectopascal { get; set; }

        [JsonProperty("humidity")]
        public decimal HumidityPercent { get; set; }

        [JsonProperty("temp_min")]
        public decimal MinimumTemperatureKelvin { get; set; }

        [JsonProperty("temp_max")]
        public decimal MaximumTemperatureKelvin { get; set; }
    }
}
