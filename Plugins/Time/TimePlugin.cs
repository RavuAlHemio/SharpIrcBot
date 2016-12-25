using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.TimeZones;
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

        protected IDateTimeZoneProvider TimeZoneProvider { get; set; }

        public TimePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new TimeConfig(config);

            LoadTimeZoneData();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new TimeConfig(newConfig);

            LoadTimeZoneData();
        }

        protected virtual void LoadTimeZoneData()
        {
            using (Stream stream = File.Open(Path.Combine(SharpIrcBotUtil.AppDirectory, Config.TimeZoneDatabaseFile), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                TzdbDateTimeZoneSource timeZoneSource = TzdbDateTimeZoneSource.FromStream(stream);
                TimeZoneProvider = new DateTimeZoneCache(timeZoneSource);
            }
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

            string aliasedLocation;
            if (Config.LocationAliases.TryGetValue(location, out aliasedLocation))
            {
                location = aliasedLocation;
            }

            // obtain location
            var geoSearchResult = new GeoSearchResult();
            using (var client = new HttpClient())
            {
                var geoSearchUri = new Uri($"http://api.geonames.org/searchJSON?maxRows=1&q={WebUtility.UrlEncode(location)}&username={WebUtility.UrlEncode(Config.GeoNamesUsername)}");
                string geoSearchResponse = client.GetStringAsync(geoSearchUri).SyncWait();
                Logger.LogDebug("geo search response: {Response}", geoSearchResponse);

                using (var sr = new StringReader(geoSearchResponse))
                {
                    JsonSerializer.Create().Populate(sr, geoSearchResult);
                }
            }

            if (!geoSearchResult.GeoNames.Any())
            {
                ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: GeoNames cannot find that location!");
                return;
            }

            GeoName loc = geoSearchResult.GeoNames[0];

            // obtain timezone
            var geoTimeZoneResult = new GeoTimeZoneResult();
            using (var client = new HttpClient())
            {
                var timeZoneSearchUri = new Uri($"http://api.geonames.org/timezoneJSON?lat={loc.Latitude}&lng={loc.Longitude}&username={WebUtility.UrlEncode(Config.GeoNamesUsername)}");
                string timeZoneSearchResponse = client.GetStringAsync(timeZoneSearchUri).SyncWait();
                Logger.LogDebug("timezone search response: {Response}", timeZoneSearchResponse);

                using (var sr = new StringReader(timeZoneSearchResponse))
                {
                    JsonSerializer.Create().Populate(sr, geoTimeZoneResult);
                }
            }

            // find actual date/time using our zone data
            DateTimeZone zone = TimeZoneProvider.GetZoneOrNull(geoTimeZoneResult.TimezoneID);
            if (zone == null)
            {
                ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: I don't know the timezone {geoTimeZoneResult.TimezoneID}.");
                return;
            }

            ZonedDateTime time = SystemClock.Instance.GetCurrentInstant().InZone(zone);

            ConnectionManager.SendChannelMessage(
                args.Channel,
                match.Groups["lucky"].Success
                    ? $"{args.SenderNickname}: The time there is {time:yyyy-MM-dd HH:mm:ss}."
                    : $"{args.SenderNickname}: The time in {geoSearchResult.GeoNames[0].Name} is {time:yyyy-MM-dd HH:mm:ss}."
            );
        }
    }
}
