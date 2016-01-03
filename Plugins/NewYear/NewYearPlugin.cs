﻿using System;
using System.Reflection;
using SharpIrcBot;
using log4net;
using Newtonsoft.Json.Linq;

namespace NewYear
{
    public class NewYearPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ConnectionManager ConnectionManager { get; }
        protected NewYearConfig Config { get; }

        public NewYearPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new NewYearConfig(config);

            ScheduleNewYear();
        }

        protected virtual void ScheduleNewYear()
        {
            // try this year
            var now = Config.LocalTime ? DateTimeOffset.Now : DateTimeOffset.UtcNow;
            var newYear = new DateTimeOffset(
                now.Year,
                Config.CustomMonth,
                Config.CustomDay,
                Config.CustomHour,
                Config.CustomMinute,
                Config.CustomSecond,
                now.Offset
            );

            if (newYear < now)
            {
                // it's gone; try next year
                newYear = new DateTimeOffset(
                    now.Year + 1,
                    Config.CustomMonth,
                    Config.CustomDay,
                    Config.CustomHour,
                    Config.CustomMinute,
                    Config.CustomSecond,
                    now.Offset
                );
            }

            // schedule for then
            ConnectionManager.Timers.Register(newYear, () => SpamNewYear(newYear.Year + Config.YearBiasToGregorian));
        }

        protected virtual void SpamNewYear(long newYearNumber)
        {
            long oldYearNumber = newYearNumber - 1;
            foreach (var channel in Config.Channels)
            {
                ConnectionManager.SendChannelMessageFormat(channel, "</{0}>", oldYearNumber);
                ConnectionManager.SendChannelMessageFormat(channel, "<{0}>", newYearNumber);
            }
        }
    }
}            
