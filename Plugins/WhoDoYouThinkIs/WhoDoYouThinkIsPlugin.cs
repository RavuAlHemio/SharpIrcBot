using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.WhoDoYouThinkIs
{
    public class WhoDoYouThinkIsPlugin : IPlugin
    {
        protected IConnectionManager ConnectionManager { get; }

        public WhoDoYouThinkIsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;

            var wdytiCommand = new Command(
                CommandUtil.MakeNames("wdyti"),
                CommandUtil.NoOptions,
                CommandUtil.MakeArguments(
                    CommandUtil.NonzeroStringMatcherRequiredWordTaker // nickname
                ),
                forbiddenFlags: MessageFlags.UserBanned
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(wdytiCommand, HandleWDYTICommandInChannel);
            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(wdytiCommand, HandleWDYTICommandInQuery);
        }

        private void HandleWDYTICommandInQuery(CommandMatch cmd, IPrivateMessageEventArgs msg)
        {
            HandleMessage(
                (string)cmd.Arguments[0],
                body => ConnectionManager.SendQueryMessage(msg.SenderNickname, body)
            );
        }

        private void HandleWDYTICommandInChannel(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            HandleMessage(
                (string)cmd.Arguments[0],
                body => ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {body}")
            );
        }

        private void HandleMessage(string nick, Action<string> respond)
        {
            string regName = ConnectionManager.RegisteredNameForNick(nick);

            respond(regName == null ? $"I don't know {nick}." : $"I think {nick} is {regName}.");
        }
    }
}
