using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.BanKit
{
    public class BanKitPlugin : IPlugin
    {
        public static readonly Regex TimeSpanRegex = new Regex(
            "^"
            + "(?:(?<days>[1-9][0-9]*)d)?"
            + "(?:(?<hours>[1-9][0-9]*)h)?"
            + "(?:(?<minutes>[1-9][0-9]*)m)?"
            + "(?:(?<seconds>[1-9][0-9]*)s)?"
            + "$",
            RegexOptions.Compiled
        );

        protected IConnectionManager ConnectionManager { get; }

        public BanKitPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;

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
            string message = ((string)commandMatch.Arguments[2]).Trim();

            string nick = null;
            if (!mask.Contains("!") && !mask.Contains("@"))
            {
                // it's a nickname
                nick = mask;
                mask += "!*@*";
            }

            TimeSpan? timeSpan = TimeSpanFromString(timeSpanString);
            if (!timeSpan.HasValue)
            {
                return;
            }

            message = (message.Length == 0)
                ? $"{msg.SenderNickname}"
                : $"{msg.SenderNickname}: {message}"
            ;

            DateTimeOffset banEndTime = DateTimeOffset.Now + timeSpan.Value;
            ConnectionManager.Timers.Register(
                banEndTime,
                () => ConnectionManager.ChangeChannelMode(msg.Channel, $"-b {mask}")
            );

            ConnectionManager.ChangeChannelMode(msg.Channel, $"+b {mask}");
            if (nick != null)
            {
                ConnectionManager.KickChannelUser(msg.Channel, nick, message);
            }
        }

        public static TimeSpan? TimeSpanFromString(string timeSpan)
        {
            Match m = TimeSpanRegex.Match(timeSpan);
            if (!m.Success)
            {
                return null;
            }

            int? days = MaybeIntFromMatchGroup(m.Groups["days"]);
            int? hours = MaybeIntFromMatchGroup(m.Groups["hours"]);
            int? minutes = MaybeIntFromMatchGroup(m.Groups["minutes"]);
            int? seconds = MaybeIntFromMatchGroup(m.Groups["seconds"]);

            // don't allow overflow into a higher segment
            if (days.HasValue)
            {
                if (hours > 24 || minutes > 60 || seconds > 60)
                {
                    return null;
                }
            }
            if (hours.HasValue)
            {
                if (minutes > 60 || seconds > 60)
                {
                    return null;
                }
            }
            if (minutes.HasValue)
            {
                if (seconds > 60)
                {
                    return null;
                }
            }

            TimeSpan ret = TimeSpan.Zero;
            if (days.HasValue)
            {
                ret = ret.Add(TimeSpan.FromDays(days.Value));
            }
            if (hours.HasValue)
            {
                ret = ret.Add(TimeSpan.FromHours(hours.Value));
            }
            if (minutes.HasValue)
            {
                ret = ret.Add(TimeSpan.FromMinutes(minutes.Value));
            }
            if (seconds.HasValue)
            {
                ret = ret.Add(TimeSpan.FromSeconds(seconds.Value));
            }
            return ret;
        }

        static int? MaybeIntFromMatchGroup(Group grp)
        {
            return grp.Success
                ? int.Parse(grp.Value)
                : (int?)null;
        }
    }
}
