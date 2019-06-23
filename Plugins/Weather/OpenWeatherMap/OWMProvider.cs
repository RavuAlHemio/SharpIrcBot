using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.Weather.OpenWeatherMap.Model;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap
{
    public class OWMProvider : IWeatherProvider
    {
        protected OWMConfig Config { get; set; }
        protected SortedSet<DateTimeOffset> LastQueries { get; set; }
        protected HttpClient Client { get; set; }

        public OWMProvider(JObject configObj)
        {
            Config = new OWMConfig();
            JsonSerializer.Create().Populate(configObj.CreateReader(), Config);

            LastQueries = new SortedSet<DateTimeOffset>();

            Client = new HttpClient();
            Client.Timeout = Config.Timeout;
        }

        protected virtual bool CheckCooldownEnough(int requiredCount = 1)
        {
            if (!Config.MaxCallsPerMinute.HasValue)
            {
                return true;
            }

            lock (LastQueries)
            {
                var now = DateTimeOffset.UtcNow;
                var minuteAgo = now.AddMinutes(-1.0);
                LastQueries.RemoveWhere(callTime => callTime < minuteAgo);
                return LastQueries.Count + requiredCount <= Config.MaxCallsPerMinute.Value;
            }
        }

        protected virtual void RegisterForCooldown()
        {
            if (!Config.MaxCallsPerMinute.HasValue)
            {
                return;
            }

            lock (LastQueries)
            {
                LastQueries.Add(DateTimeOffset.UtcNow);
            }
        }

        public string GetWeatherDescriptionForCoordinates(decimal latitudeDegNorth, decimal longitudeDegEast)
        {
            if (!CheckCooldownEnough(2))
            {
                return "I'm on cooldown. :(";
            }

            string weatherUri = string.Format(
                CultureInfo.InvariantCulture,
                "https://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&appid={2}",
                latitudeDegNorth, longitudeDegEast, Config.ApiKey
            );
            string weatherJsonText = Client
                .GetStringAsync(weatherUri)
                .Result;
            RegisterForCooldown();

            var currentWeather = new OWMWeatherState();
            JsonConvert.PopulateObject(weatherJsonText, currentWeather);

            string forecastUri = string.Format(
                CultureInfo.InvariantCulture,
                "https://api.openweathermap.org/data/2.5/forecast?lat={0}&lon={1}&appid={2}",
                latitudeDegNorth, longitudeDegEast, Config.ApiKey
            );
            string forecastJsonText = Client
                .GetStringAsync(weatherUri)
                .Result;
            RegisterForCooldown();

            var forecast = new OWMForecast();
            JsonConvert.PopulateObject(forecastJsonText, forecast);

            return $"Current temperature: {KelvinToCelsius(currentWeather.Main.TemperatureKelvin)} °C";
        }

        public static decimal KelvinToCelsius(decimal kelvin)
        {
            return kelvin - 273.15m;
        }
    }
}
