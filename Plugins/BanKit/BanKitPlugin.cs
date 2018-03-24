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

            TimeSpan? timeSpan = TimeUtil.TimeSpanFromString(timeSpanString);
            if (!timeSpan.HasValue || timeSpan.Value == TimeSpan.Zero)
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
    }
}
