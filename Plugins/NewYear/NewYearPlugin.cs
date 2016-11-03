using System;
using Microsoft.Extensions.Logging;
using SharpIrcBot;
using Newtonsoft.Json.Linq;

namespace NewYear
{
    public class NewYearPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<NewYearPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected NewYearConfig Config { get; set; }

        public NewYearPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new NewYearConfig(config);

            ScheduleNewYear();
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new NewYearConfig(newConfig);
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
