using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events.Irc;
using Time.GeoNames;

namespace Time
{
    public class TimePlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<TimePlugin>();

        public static readonly Regex TimeRegex = new Regex("^!(?<lucky>l)?time(?:\\s+(?<location>\\S+(?:\\s+\\S+)*))?\\s*$", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager { get; }
        protected TimeConfig Config { get; set; }

        public TimePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new TimeConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new TimeConfig(newConfig);
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var match = TimeRegex.Match(args.Message);
            if (!match.Success)
            {
                return;
            }

            string location = match.Groups["location"].Success
                ? match.Groups["location"].Value
                : Config.DefaultLocation;

            // obtain location
            var geoSearchResult = new GeoSearchResult();
            using (var client = new HttpClient())
            {
                var geoSearchUri = new Uri($"http://api.geonames.org/searchJSON?maxRows=1&q={WebUtility.UrlEncode(location)}&username={WebUtility.UrlEncode(Config.GeoNamesUsername)}");
                string geoSearchResponse = client.GetStringAsync(geoSearchUri).SyncWait();
                Logger.LogDebug($"geo search response: {geoSearchResponse}");

                using (var sr = new StringReader(geoSearchResponse))
                {
                    JsonSerializer.Create().Populate(sr, geoSearchResult);
                }
            }

            if (!geoSearchResult.GeoNames.Any())
            {
                ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: GeoNames cannot found that location!");
                return;
            }

            GeoName loc = geoSearchResult.GeoNames[0];

            // obtain timezone
            var geoTimeZoneResult = new GeoTimeZoneResult();
            using (var client = new HttpClient())
            {
                var timeZoneSearchUri = new Uri($"http://api.geonames.org/timezoneJSON?lat={loc.Latitude}&lng={loc.Longitude}&username={WebUtility.UrlEncode(Config.GeoNamesUsername)}");
                string timeZoneSearchResponse = client.GetStringAsync(timeZoneSearchUri).SyncWait();
                Logger.LogDebug($"timezone search response: {timeZoneSearchResponse}");

                using (var sr = new StringReader(timeZoneSearchResponse))
                {
                    JsonSerializer.Create().Populate(sr, geoTimeZoneResult);
                }
            }

            DateTime time = geoTimeZoneResult.Time.AddSeconds(DateTime.Now.Second);

            ConnectionManager.SendChannelMessage(
                args.Channel,
                match.Groups["lucky"].Success
                    ? $"{args.SenderNickname}: The time there is {time:yyyy-MM-dd HH:mm:ss}."
                    : $"{args.SenderNickname}: The time in {location} is {time:yyyy-MM-dd HH:mm:ss}."
            );
        }
    }
}
