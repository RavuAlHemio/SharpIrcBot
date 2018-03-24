﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Libraries.GeoNames;
using SharpIrcBot.Plugins.Weather.Wunderground;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Weather
{
    public class WeatherPlugin : IPlugin, IReloadableConfiguration
    {
        protected static readonly Regex LatLonRegex = new Regex("^\\s*[0-9]+(?:[.][0-9]*)?,\\s*[0-9]+(?:[.][0-9]*)?\\s*$");

        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<WeatherPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected WeatherConfig Config { get; set; }
        protected WundergroundClient Client { get; set; }

        // rate-limiting stuff
        protected SortedSet<DateTimeOffset> PreviousRequests { get; }
        protected DateTimeOffset? LastRequestDayEST { get; set; }
        protected int RequestsTodayEST { get; set; }
        protected Random RNG { get; set; }

        public WeatherPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new WeatherConfig(config);
            Client = new WundergroundClient
            {
                Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds)
            };

            PreviousRequests = new SortedSet<DateTimeOffset>();
            LastRequestDayEST = null;
            RequestsTodayEST = 0;
            RNG = new Random();

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("weather", "lweather"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // location
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleWeatherCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new WeatherConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            // a new client must be created if a request has already been performed
            Client = new WundergroundClient
            {
                Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds)
            };
        }

        protected virtual DateTime CalculateTodayEST()
        {
            return DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(-5.0)).Date;
        }

        protected virtual bool CheckIsCoolEnough()
        {
            if (Config.MaxRequestsPerESTDay > 0 && LastRequestDayEST.HasValue)
            {
                // we remember a last request and we have a max requests per EST day limit
                if (LastRequestDayEST.Value == CalculateTodayEST())
                {
                    // last request was today (EST)
                    if (RequestsTodayEST >= Config.MaxRequestsPerESTDay)
                    {
                        // too many requests today
                        return false;
                    }
                }
            }

            if (Config.MaxRequestsPerMinute > 0)
            {
                // we have a max requests per minute limit
                if (PreviousRequests.Count > 0)
                {
                    // we remember a previous request

                    // remove all entries older than a minute
                    var oneMinuteAgo = DateTimeOffset.Now.AddMinutes(-1.0);
                    PreviousRequests.RemoveWhere(timestamp => timestamp < oneMinuteAgo);

                    if (PreviousRequests.Count >= Config.MaxRequestsPerMinute)
                    {
                        // too many requests in the last 60min
                        return false;
                    }
                }
            }

            // okay, we're fine
            return true;
        }

        protected virtual void GetWeatherForLocation(string location, string channel, string nick, bool lucky, bool lookupAlias = true)
        {
            if (!CheckIsCoolEnough())
            {
                string coolDownResponse;
                if (Config.CoolDownResponses.Any())
                {
                    coolDownResponse = Config.CoolDownResponses[RNG.Next(Config.CoolDownResponses.Count)];
                }
                else
                {
                    coolDownResponse = "I'm being rate-limited; sorry!";
                }

                ConnectionManager.SendChannelMessage(channel, $"{nick}: {coolDownResponse}");
                return;
            }

            if (lookupAlias)
            {
                string aliasedLocation;
                if (Config.LocationAliases.TryGetValue(location, out aliasedLocation))
                {
                    location = aliasedLocation;
                }
            }

            // find the location using GeoNames (Wunderground's geocoding is really bad)
            if (!location.StartsWith("pws:") && !location.StartsWith("zmw:") && !LatLonRegex.IsMatch(location))
            {
                var geoClient = new GeoNamesClient(Config.GeoNames);
                GeoName loc = geoClient.GetFirstGeoName(location).SyncWait();
                if (loc == null)
                {
                    ConnectionManager.SendChannelMessage(channel, $"{nick}: GeoNames cannot find that location!");
                    return;
                }
                location = Inv($"{loc.Latitude},{loc.Longitude}");
            }

            // obtain weather info
            WundergroundResponse response;
            try
            {
                response = Client.GetWeatherForLocation(Config.WunderApiKey, location);
            }
            catch (AggregateException ae)
            {
                Type innerType = ae.InnerException.GetType();
                if (innerType == typeof(TaskCanceledException))
                {
                    ConnectionManager.SendChannelMessage(channel, $"{nick}: Wunderground request timed out!");
                    return;
                }
                else if (innerType == typeof(HttpRequestException))
                {
                    var we = (HttpRequestException)ae.InnerException;
                    Logger.LogWarning("error fetching Wunderground result: {Exception}", we);
                    ConnectionManager.SendChannelMessage(channel, $"{nick}: Error obtaining Wunderground response!");
                    return;
                }
                else
                {
                    throw;
                }
            }

            // register cooldown-relevant stuff
            PreviousRequests.Add(DateTimeOffset.Now);
            var todayEST = CalculateTodayEST();
            if (LastRequestDayEST == todayEST)
            {
                ++RequestsTodayEST;
            }
            else
            {
                LastRequestDayEST = todayEST;
                RequestsTodayEST = 1;
            }

            if (response.Metadata.LocationMatches?.Count > 1)
            {
                // pick the first
                GetWeatherForLocation("zmw:" + response.Metadata.LocationMatches.First().WundergroundLocationID, channel, nick, lucky);
                return;
            }

            if (response.Metadata.Error != null)
            {
                if (response.Metadata.Error.Type == "querynotfound")
                {
                    ConnectionManager.SendChannelMessage(channel, $"{nick}: Wunderground cannot find that location.");
                }
                else
                {
                    ConnectionManager.SendChannelMessage(channel, $"{nick}: Something went wrong!");
                    Logger.LogError(
                        "Wunderground error of type {ErrorType} with description: {ErrorDescription}",
                        response.Metadata.Error.Type, response.Metadata.Error.Description
                    );
                }
                return;
            }

            var weather = new StringBuilder();
            weather.Append($"{nick}: ");
            if (!lucky && response.CurrentWeather?.DisplayLocation.FullName != null)
            {
                weather.Append($"{response.CurrentWeather.DisplayLocation.FullName}: ");
            }

            var pieces = new List<string>();

            if (!string.IsNullOrWhiteSpace(response.CurrentWeather?.WeatherDescription))
            {
                pieces.Add(response.CurrentWeather.WeatherDescription);
            }

            if ((response.CurrentWeather?.Temperature).HasValue)
            {
                if ((response.CurrentWeather?.FeelsLikeTemperature).HasValue)
                {
                    pieces.Add($"{response.CurrentWeather.Temperature}°C (feels like {response.CurrentWeather.FeelsLikeTemperature}°C)");
                }
                else
                {
                    pieces.Add($"{response.CurrentWeather.Temperature}°C");
                }
            }
            else if ((response.CurrentWeather?.FeelsLikeTemperature).HasValue)
            {
                pieces.Add($"feels like {response.CurrentWeather.FeelsLikeTemperature}°C");
            }

            if (!string.IsNullOrWhiteSpace(response.CurrentWeather?.Humidity))
            {
                pieces.Add($"{response.CurrentWeather.Humidity} humidity");
            }

            if (pieces.Count == 0)
            {
                pieces.Add("current weather unknown");
            }

            weather.Append(pieces.StringJoin(", "));

            if (response.Forecast?.Simple?.Days != null && response.Forecast.Simple.Days.Count > 0)
            {
                weather.Append("; forecast: ");
                var forecastBits = new List<string>();
                foreach (var day in response.Forecast.Simple.Days)
                {
                    bool anything = false;
                    var bit = new StringBuilder($"{day.Date.WeekdayShort.Substring(0, 2)} {day.Date.Day:D1}.{day.Date.Month:D2}.");
                    if (day.Conditions != null)
                    {
                        bit.Append($" {day.Conditions}");
                        anything = true;
                    }
                    if (day.LowTemperature.Celsius.HasValue && day.HighTemperature.Celsius.HasValue)
                    {
                        bit.Append($" {day.LowTemperature.Celsius}\u2013{day.HighTemperature.Celsius}°C");
                        anything = true;
                    }
                    else if (day.LowTemperature.Celsius.HasValue)
                    {
                        bit.Append($" \u2265{day.LowTemperature.Celsius}°C");
                        anything = true;
                    }
                    else if (day.HighTemperature.Celsius.HasValue)
                    {
                        bit.Append($" \u2264{day.HighTemperature.Celsius}°C");
                        anything = true;
                    }

                    if (anything)
                    {
                        forecastBits.Add(bit.ToString());
                    }
                }
                weather.Append(forecastBits.StringJoin(", "));
            }

            if (response.CurrentWeather != null && response.CurrentWeather.LastUpdateUnixTimestamp.HasValue)
            {
                DateTimeOffset lastUpdate =
                    DateTimeOffset.FromUnixTimeSeconds(response.CurrentWeather.LastUpdateUnixTimestamp.Value);
                DateTimeOffset now = DateTimeOffset.Now;
                TimeSpan delta = lastUpdate - now;
                weather.AppendFormat("; last updated {0}", FormatTimeSpan(delta));
            }

            ConnectionManager.SendChannelMessage(channel, weather.ToString());
        }

        protected virtual void HandleWeatherCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string location = ((string)cmd.Arguments[0]).Trim();
            if (location.Length == 0)
            {
                location = Config.DefaultLocation;
            }

            GetWeatherForLocation(location, msg.Channel, msg.SenderNickname, cmd.CommandName[0] == 'l');
        }

        protected virtual string FormatTimeSpan(TimeSpan span)
        {
            return FormatTimeSpanImpl(span);
        }

        internal static string FormatTimeSpanImpl(TimeSpan span)
        {
            bool ago = false;

            if (span.Ticks < 0)
            {
                span = span.Negate();
                ago = true;
            }

            if (span.TotalSeconds < 1.0)
            {
                return "now";
            }

            var oTemporaOMores = new List<Tuple<long, string>>
            {
                TT(span.Days, "day", "days"),
                TT(span.Hours, "hour", "hours"),
                TT(span.Minutes, "minute", "minutes"),
                TT(span.Seconds, "second", "seconds")
            };

            // remove the empty large units
            while (oTemporaOMores.Count > 0 && oTemporaOMores[0].Item1 == 0)
            {
                oTemporaOMores.RemoveAt(0);
            }

            // show two consecutive units at most
            if (oTemporaOMores.Count > 2)
            {
                oTemporaOMores.RemoveRange(2, oTemporaOMores.Count - 2);
            }

            // delete the second unit if it is zero
            if (oTemporaOMores.Count > 1 && oTemporaOMores[0].Item1 == 0)
            {
                oTemporaOMores.RemoveAt(1);
            }

            // fun!
            string joint = oTemporaOMores.Select(t => t.Item2).StringJoin(" ");
            return ago
                ? (joint + " ago")
                : ("in " + joint)
            ;
        }

        // "time tuple"
        private static Tuple<long, string> TT(long time, string singular, string plural)
            => Tuple.Create(time, string.Format("{0} {1}", time, (time == 1) ? singular : plural));

        private static string Inv(FormattableString formattable)
        {
            return formattable.ToString(CultureInfo.InvariantCulture);
        }
    }
}
