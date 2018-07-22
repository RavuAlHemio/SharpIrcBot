using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.BanKit.ORM;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.BanKit
{
    public class BanKitPlugin : IPlugin, IReloadableConfiguration
    {
        protected BanKitConfig Config { get; set; }
        protected IConnectionManager ConnectionManager { get; }

        public BanKitPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new BanKitConfig(config);

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("tb", "timeban", "timedban", "tkb"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // nickname
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // timespan
                        RestTaker.Instance // message
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleTimedBanCommand
            );

            if (Config.PersistBans)
            {
                // load persisted bans
                List<BanEntry> entriesToSchedule;
                using (BanKitContext ctx = GetNewContext())
                {
                    entriesToSchedule = ctx.BanEntries
                        .Where(be => !be.Lifted)
                        .ToList();
                }

                foreach (BanEntry ban in entriesToSchedule)
                {
                    if (ban.TimestampBanEnd <= DateTimeOffset.Now)
                    {
                        // unban immediately
                        UnbanChannelMask(ban.Channel, ban.BannedMask);
                    }
                    else
                    {
                        // schedule for later
                        ConnectionManager.Timers.Register(
                            ban.TimestampBanEnd,
                            () => UnbanChannelMask(ban.Channel, ban.BannedMask)
                        );
                    }
                }
            }
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new BanKitConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleTimedBanCommand(CommandMatch commandMatch, IChannelMessageEventArgs msg)
        {
            ChannelUserLevel level = ConnectionManager.GetChannelLevelForUser(msg.Channel, msg.SenderNickname);
            if (level < ChannelUserLevel.HalfOp)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: You need to be a channel operator.");
                return;
            }

            var mask = (string)commandMatch.Arguments[0];
            var timeSpanString = (string)commandMatch.Arguments[1];
            string reason = ((string)commandMatch.Arguments[2]).Trim();

            if (reason.Length == 0)
            {
                reason = null;
            }

            string nick = null;
            if (!mask.Contains("!") && !mask.Contains("@"))
            {
                // it's a nickname
                nick = mask;
                mask += "!*@*";
            }

            TimeSpan? timeSpan = TimeUtil.TimeSpanFromString(timeSpanString);
            if (!timeSpan.HasValue || timeSpan.Value == TimeSpan.Zero)
            {
                return;
            }

            string message = (reason == null)
                ? $"{msg.SenderNickname}"
                : $"{msg.SenderNickname}: {reason}"
            ;

            DateTimeOffset banStartTime = DateTimeOffset.Now;
            DateTimeOffset banEndTime = banStartTime + timeSpan.Value;
            ConnectionManager.Timers.Register(
                banEndTime,
                () => UnbanChannelMask(msg.Channel, mask)
            );

            ConnectionManager.ChangeChannelMode(msg.Channel, $"+b {mask}");
            if (nick != null)
            {
                ConnectionManager.KickChannelUser(msg.Channel, nick, message);
            }

            if (Config.PersistBans)
            {
                using (BanKitContext ctx = GetNewContext())
                {
                    var ban = new BanEntry
                    {
                        BannedNick = nick,
                        BannedMask = mask,
                        BannerNick = msg.SenderNickname,
                        Channel = msg.Channel,
                        TimestampBanStart = banStartTime,
                        TimestampBanEnd = banEndTime,
                        Reason = reason,
                        Lifted = false,
                    };
                    ctx.BanEntries.Add(ban);

                    ctx.SaveChanges();
                }
            }
        }

        protected virtual void UnbanChannelMask(string channel, string mask)
        {
            ConnectionManager.ChangeChannelMode(channel, $"-b {mask}");

            if (Config.PersistBans)
            {
                using (BanKitContext ctx = GetNewContext())
                {
                    IEnumerable<BanEntry> bansToLift = ctx.BanEntries
                        .Where(be => be.BannedMask == mask && !be.Lifted);
                    foreach (BanEntry ban in bansToLift)
                    {
                        ban.Lifted = true;
                    }
                    ctx.SaveChanges();
                }
            }
        }

        private BanKitContext GetNewContext()
        {
            var opts = DatabaseUtil.GetContextOptions<BanKitContext>(Config);
            return new BanKitContext(opts);
        }
    }
}
