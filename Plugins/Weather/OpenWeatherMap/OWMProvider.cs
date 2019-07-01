using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.Weather.OpenWeatherMap.Model;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap
{
    public class OWMProvider : IWeatherProvider
    {
        public static readonly Regex WeatherStationRegex = new Regex("^owm:ws:(?<id>[0-9a-f]+)$", RegexOptions.Compiled);

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

        protected virtual string GetAndPopulateJson(string uri, object target)
        {
            HttpResponseMessage response = Client
                .GetAsync(uri)
                .Result;
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return "OpenWeatherMap is currently unavailable. :(";
            }
            string jsonText = response.Content
                .ReadAsStringAsync()
                .Result;

            JsonConvert.PopulateObject(jsonText, target);
            return null;
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
            var currentWeather = new OWMWeatherState();
            string err = GetAndPopulateJson(weatherUri, currentWeather);
            RegisterForCooldown();
            if (err != null)
            {
                return err;
            }

            string forecastUri = string.Format(
                CultureInfo.InvariantCulture,
                "https://api.openweathermap.org/data/2.5/forecast?lat={0}&lon={1}&appid={2}",
                latitudeDegNorth, longitudeDegEast, Config.ApiKey
            );
            var forecast = new OWMForecast();
            err = GetAndPopulateJson(forecastUri, forecast);
            RegisterForCooldown();
            if (err != null)
            {
                return err;
            }

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

        public string GetWeatherDescriptionForSpecial(string specialString)
        {
            Match wsMatch = WeatherStationRegex.Match(specialString);
            if (wsMatch.Success)
            {
                string wsID = wsMatch.Groups["id"].Value;
                return GetWeatherDescriptionForWeatherStation(wsID);
            }

            return null;
        }

        protected virtual string GetWeatherDescriptionForWeatherStation(string weatherStationID)
        {
            long nowTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            long oneHourAgoTime = nowTime - (60*60);

            string weatherUri = string.Format(
                CultureInfo.InvariantCulture,
                "https://api.openweathermap.org/data/3.0/measurements?station_id={0}&type=m&limit=10&from={1}&to={2}&appid={3}",
                weatherStationID, oneHourAgoTime, nowTime, Config.ApiKey
            );
            var readings = new List<OWMStationReading>();
            string err = GetAndPopulateJson(weatherUri, readings);
            RegisterForCooldown();
            if (err != null)
            {
                return err;
            }

            if (!readings.Any())
            {
                return "OpenWeatherMap returned no readings for this weather station!";
            }

            var builder = new StringBuilder();
            OWMStationReading newestReading = readings
                .OrderByDescending(r => r.Timestamp)
                .First();

            // current temperature
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0.0} \u00B0C",
                newestReading.Temperature.AverageValueCelsius
            );

            // current humidity
            if (builder.Length > 0)
            {
                builder.Append(", ");
            }
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0}% humidity",
                newestReading.Humidity.AverageValuePercent
            );

            // append time info
            TimeSpan timeDiff = newestReading.Timestamp - DateTimeOffset.Now;
            builder.AppendFormat(" ({0})", FormatTimeSpan(timeDiff));

            return "OpenWeatherMap: " + builder.ToString();
        }

        protected virtual string FormatTimeSpan(TimeSpan span)
        {
            return WeatherPlugin.FormatTimeSpanImpl(span);
        }
    }
}
