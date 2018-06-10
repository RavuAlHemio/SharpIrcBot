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

            // it's a fun command in a channel and a useful command in private message
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("wdyti"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // nickname
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleWDYTICommandInChannel
            );
            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("wdyti"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // nickname
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleWDYTICommandInQuery
            );
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
