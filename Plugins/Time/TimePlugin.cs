using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.TimeZones;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Libraries.GeoNames;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Time
{
    public class TimePlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<TimePlugin>();

        public const long TicksPerSecond = 10_000_000;

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
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleTimeCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("interval"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // date/time
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleIntervalCommand
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
            GeoName loc = client.GetFirstGeoName(location).SyncWait();
            if (loc == null)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: GeoNames cannot find that location!");
                return;
            }

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
                    : $"{msg.SenderNickname}: The time in {loc.Name} is {time:yyyy-MM-dd HH:mm:ss}."
            );
        }

        static void MaybeAddUnit(List<string> pieces, int value, string singular, string plural)
        {
            if (value == 1)
            {
                pieces.Add($"1 {singular}");
            }
            else if (value != 0)
            {
                pieces.Add($"{value} {plural}");
            }
        }

        protected virtual void HandleIntervalCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            var dateTimeString = (string)cmd.Arguments[0];
            DateTime? timestamp = TimeUtil.DateTimeFromString(dateTimeString);
            if (!timestamp.HasValue)
            {
                return;
            }

            DateTime timestampUTC = timestamp.Value.ToUniversalTime();
            DateTime nowUTC = DateTime.UtcNow;
            DateTime nowUTCFullSeconds = nowUTC.AddTicks(-(nowUTC.Ticks % TicksPerSecond));

            CalendarTimeSpan diff = TimeComparison.CalendarDifference(nowUTCFullSeconds, timestampUTC);

            var pieces = new List<string>();
            MaybeAddUnit(pieces, diff.Years, "year", "years");
            MaybeAddUnit(pieces, diff.Months, "month", "months");
            MaybeAddUnit(pieces, diff.Days, "day", "days");
            MaybeAddUnit(pieces, diff.Hours, "hour", "hours");
            MaybeAddUnit(pieces, diff.Minutes, "minute", "minutes");
            MaybeAddUnit(pieces, diff.Seconds, "second", "seconds");

            string message;
            if (pieces.Count == 0)
            {
                message = "That’s now!";
            }
            else
            {
                var messageBuilder = new StringBuilder();

                if (pieces.Count > 1)
                {
                    messageBuilder.Append(string.Join(", ", pieces.Take(pieces.Count - 1)));
                    // 1 year, 2 months, 3 days[ and 4 hours]

                    messageBuilder.Append(" and ");
                    // 1 year, 2 months, 3 days and [4 hours]
                }
                messageBuilder.Append(pieces.Last());
                messageBuilder.Append(diff.Negative ? " ago." : " remaining.");

                message = messageBuilder.ToString();
            }

            ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {message}");
        }
    }
}
