using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Weather
{
    public class WeatherPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ConnectionManager ConnectionManager { get; }
        protected WeatherConfig Config { get; set; }
        protected WundergroundClient Client { get; set; }

        // rate-limiting stuff
        protected SortedSet<DateTimeOffset> PreviousRequests { get; }
        protected DateTimeOffset? LastRequestDayEST { get; set; }
        protected int RequestsTodayEST { get; set; }
        protected Random RNG { get; set; }

        public WeatherPlugin(ConnectionManager connMgr, JObject config)
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

        protected virtual void ActuallyHandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (!args.Data.Message.StartsWith("!weather "))
            {
                return;
            }

            var location = args.Data.Message.Substring(("!weather ").Length).Trim();
            if (location.Length == 0)
            {
                return;
            }

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

                ConnectionManager.SendChannelMessage(args.Data.Channel, coolDownResponse);
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
                // FIXME: make a choice available?
                ConnectionManager.SendChannelMessage(args.Data.Channel, "More than one location matched; please make your query more precise!");
                return;
            }

            if (response.Error != null)
            {
                if (response.Error.Type == "querynotfound")
                {
                    ConnectionManager.SendChannelMessage(args.Data.Channel, "Location not found.");
                }
                else
                {
                    ConnectionManager.SendChannelMessage(args.Data.Channel, "Something went wrong!");
                    Logger.Error($"Wunderground error of type {response.Error.Type} with description: {response.Error.Description}");
                }
                return;
            }

            string weather = $"{response.CurrentWeather.WeatherDescription}, {response.CurrentWeather.Temperature}°C (feels like {response.CurrentWeather.FeelsLikeTemperature}°C), {response.CurrentWeather.Humidity} humidity";
            ConnectionManager.SendChannelMessage(args.Data.Channel, weather);
        }
    }
}
