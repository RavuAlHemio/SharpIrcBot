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
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Libraries.GeoNames;

namespace SharpIrcBot.Plugins.Time
{
    public class TimePlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<TimePlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected TimeConfig Config { get; set; }

        protected IDateTimeZoneProvider TimeZoneProvider { get; set; }

        public TimePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new TimeConfig(config);

            LoadTimeZoneData();

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("time", "ltime"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // location
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleTimeCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new TimeConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
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

        protected virtual void HandleTimeCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string location = ((string)cmd.Arguments[0]).Trim();
            if (location.Length == 0)
            {
                location = Config.DefaultLocation;
            }

            string aliasedLocation;
            if (Config.LocationAliases.TryGetValue(location, out aliasedLocation))
            {
                location = aliasedLocation;
            }

            // obtain location
            var client = new GeoNamesClient(Config.GeoNames);
            GeoSearchResult geoSearchResult = client.SearchForLocation(location).SyncWait();

            if (!geoSearchResult.GeoNames.Any())
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: GeoNames cannot find that location!");
                return;
            }

            GeoName loc = geoSearchResult.GeoNames[0];

            // obtain timezone
            GeoTimeZoneResult geoTimeZoneResult = client.GetTimezone(loc.Latitude, loc.Longitude).SyncWait();

            // find actual date/time using our zone data
            DateTimeZone zone = TimeZoneProvider.GetZoneOrNull(geoTimeZoneResult.TimezoneID);
            if (zone == null)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: I don't know the timezone {geoTimeZoneResult.TimezoneID}.");
                return;
            }

            ZonedDateTime time = SystemClock.Instance.GetCurrentInstant().InZone(zone);

            bool lucky = cmd.CommandName.StartsWith("l");
            ConnectionManager.SendChannelMessage(
                msg.Channel,
                lucky
                    ? $"{msg.SenderNickname}: The time there is {time:yyyy-MM-dd HH:mm:ss}."
                    : $"{msg.SenderNickname}: The time in {geoSearchResult.GeoNames[0].Name} is {time:yyyy-MM-dd HH:mm:ss}."
            );
        }
    }
}
