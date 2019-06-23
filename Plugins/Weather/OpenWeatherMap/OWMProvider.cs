using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.Weather.OpenWeatherMap.Model;
using SharpIrcBot.Util;

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

        protected virtual SortedDictionary<DateTime, OWMForecastSummary> SummarizeForecast(OWMForecast forecast)
        {
            var ret = new SortedDictionary<DateTime, OWMForecastSummary>();

            foreach (OWMWeatherState state in forecast.WeatherStates)
            {
                DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(state.UnixTimestamp);
                DateTime date = timestamp.Date;

                OWMForecastSummary curSummary;
                if (ret.TryGetValue(date, out curSummary))
                {
                    curSummary.MaxTempKelvin = Math.Max(curSummary.MaxTempKelvin, state.Main.MaximumTemperatureKelvin);
                    curSummary.MinTempKelvin = Math.Min(curSummary.MinTempKelvin, state.Main.MinimumTemperatureKelvin);
                }
                else
                {
                    curSummary = new OWMForecastSummary
                    {
                        MaxTempKelvin = state.Main.MaximumTemperatureKelvin,
                        MinTempKelvin = state.Main.MinimumTemperatureKelvin,
                        WeatherStates = new List<string>(),
                    };
                    ret[date] = curSummary;
                }

                foreach (OWMWeather weather in state.Weathers)
                {
                    if (!curSummary.WeatherStates.Contains(weather.Main))
                    {
                        curSummary.WeatherStates.Add(weather.Main);
                    }
                }
            }

            return ret;
        }

        public string GetWeatherDescriptionForCoordinates(decimal latitudeDegNorth, decimal longitudeDegEast)
        {
            if (!CheckCooldownEnough(2))
            {
                return "OpenWeatherMap is on cooldown. :(";
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
                .GetStringAsync(forecastUri)
                .Result;
            RegisterForCooldown();

            var forecast = new OWMForecast();
            JsonConvert.PopulateObject(forecastJsonText, forecast);

            var builder = new StringBuilder();

            // weather status
            if (currentWeather.Weathers.Count > 0)
            {
                builder.Append(currentWeather.Weathers[0].Main);
            }

            // current temperature
            if (builder.Length > 0)
            {
                builder.Append(", ");
            }
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0} \u00B0C",
                KelvinToCelsius(currentWeather.Main.TemperatureKelvin)
            );

            // current humidity
            if (builder.Length > 0)
            {
                builder.Append(", ");
            }
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0}% humidity",
                currentWeather.Main.HumidityPercent
            );

            if (forecast.WeatherStates.Count > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append("; ");
                }
                builder.Append("forecast: ");

                SortedDictionary<DateTime, OWMForecastSummary> summarized = SummarizeForecast(forecast);
                string forecastString = summarized
                    .Select(kvp => string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1}.{2:00}. {3} {4:0}\u2013{5:0} \u00B0C",
                        kvp.Key.DayOfWeek.ToString().Substring(0, 2),
                        kvp.Key.Day,
                        kvp.Key.Month,
                        kvp.Value.WeatherStates.StringJoin("/"),
                        KelvinToCelsius(kvp.Value.MinTempKelvin),
                        KelvinToCelsius(kvp.Value.MaxTempKelvin)
                    ))
                    .StringJoin(", ");
                builder.Append(forecastString);
            }

            return "OpenWeatherMap: " + builder.ToString();
        }

        public static decimal KelvinToCelsius(decimal kelvin)
        {
            return kelvin - 273.15m;
        }
    }
}
