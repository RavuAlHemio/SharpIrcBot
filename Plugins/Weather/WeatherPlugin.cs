using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Weather
{
    public class WeatherPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected static readonly Regex WeatherRegex = new Regex("^!(?<lucky>l)?weather(?:\\s+(?<location>.+))?\\s*$");

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
            Client = new WundergroundClient();

            PreviousRequests = new SortedSet<DateTimeOffset>();
            LastRequestDayEST = null;
            RequestsTodayEST = 0;
            RNG = new Random();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new WeatherConfig(newConfig);
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
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

        protected virtual void GetWeatherForLocation(string location, string channel, string nick, bool lucky)
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

            // obtain weather info
            var response = Client.GetWeatherForLocation(Config.WunderApiKey, location);

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
                    Logger.Error($"Wunderground error of type {response.Metadata.Error.Type} with description: {response.Metadata.Error.Description}");
                }
                return;
            }

            var weather = new StringBuilder();
            weather.Append($"{nick}: ");
            if (!lucky && response.CurrentWeather?.DisplayLocation.FullName != null)
            {
                weather.Append($"{response.CurrentWeather.DisplayLocation.FullName}: ");
            }
            if (!string.IsNullOrWhiteSpace(response.CurrentWeather?.WeatherDescription))
            {
                weather.Append($"{response.CurrentWeather.WeatherDescription}, ");
            }
            weather.Append($"{response.CurrentWeather.Temperature}°C (feels like {response.CurrentWeather.FeelsLikeTemperature}°C), {response.CurrentWeather.Humidity} humidity");

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
                weather.Append(string.Join(", ", forecastBits));
            }

            ConnectionManager.SendChannelMessage(channel, weather.ToString());
        }

        protected virtual void ActuallyHandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var match = WeatherRegex.Match(args.Data.Message);
            if (!match.Success)
            {
                return;
            }

            string location = match.Groups["location"].Success
                ? match.Groups["location"].Value
                : Config.DefaultLocation;

            GetWeatherForLocation(location, args.Data.Channel, args.Data.Nick, match.Groups["lucky"].Success);
        }
    }
}
