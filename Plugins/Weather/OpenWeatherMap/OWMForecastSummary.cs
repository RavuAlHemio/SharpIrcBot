using System.Collections.Generic;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap
{
    public class OWMForecastSummary
    {
        public decimal MinTempKelvin;
        public decimal MaxTempKelvin;
        public List<string> WeatherStates;
    }
}
