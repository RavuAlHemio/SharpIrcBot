using System;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OWMStationReading
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("date")]
        public long UnixTimestamp
        {
            get { return Timestamp.ToUnixTimeSeconds(); }
            set { Timestamp = DateTimeOffset.FromUnixTimeSeconds(value); }
        }

        [JsonIgnore]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("station_id")]
        public string StationID { get; set; }

        [JsonProperty("temp")]
        public TemperatureObject Temperature { get; set; }

        [JsonProperty("humidity")]
        public HumidityObject Humidity { get; set; }

        [JsonObject(MemberSerialization.OptIn)]
        public class TemperatureObject
        {
            [JsonProperty("max")]
            public decimal MaximumValueCelsius { get; set; }

            [JsonProperty("min")]
            public decimal MinimumValueCelsius { get; set; }

            [JsonProperty("average")]
            public decimal AverageValueCelsius { get; set; }

            [JsonProperty("weight")]
            public int WeightValue { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class HumidityObject
        {
            [JsonProperty("average")]
            public decimal AverageValuePercent { get; set; }

            [JsonProperty("weight")]
            public int WeightValue { get; set; }
        }

        // TODO: wind
        // TODO: pressure
        // TODO: precipitation
    }
}
