using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.IKnewThat.ORM;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.IKnewThat
{
    public class IKnewThatPlugin : IPlugin
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<IKnewThatPlugin>();

        protected IKnewThatConfig Config { get; set; }
        protected IConnectionManager ConnectionManager;

        public IKnewThatPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new IKnewThatConfig(config);

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("iknewthat"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // keyword
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleIKnewThatChannelCommand
            );
            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("iknowthat"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // keyword
                        RestTaker.Instance // description
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleIKnowThatQueryCommand
            );
        }

        protected virtual void HandleIKnewThatChannelCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            string senderLower = (ConnectionManager.RegisteredNameForNick(args.SenderNickname) ?? args.SenderNickname)
                .ToLowerInvariant();
            string keywordLower = ((string)cmd.Arguments[0]).ToLowerInvariant();

            using (var ctx = GetNewContext())
            {
                IKnewThatEntry matchingEntry = ctx.Entries
                    .FirstOrDefault(e => e.AuthorLowercase == senderLower && e.KeywordLowercase == keywordLower);

                if (matchingEntry == null)
                {
                    ConnectionManager.SendChannelMessage(
                        args.Channel,
                        $"{args.SenderNickname}: No, you didn't!"
                    );
                    return;
                }

                DateTimeOffset timestampLocal = matchingEntry.Timestamp.ToLocalTime();

                ConnectionManager.SendChannelMessage(
                    args.Channel,
                    $"I confirm that on {timestampLocal:yyyy-MM-dd} at {timestampLocal:HH:mm:ss}, {args.SenderNickname} knew the following: {matchingEntry.Message}"
                );

                ctx.Entries.Remove(matchingEntry);
                ctx.SaveChanges();
            }
        }

        protected virtual void HandleIKnowThatQueryCommand(CommandMatch cmd, IPrivateMessageEventArgs args)
        {
            string senderLower = (ConnectionManager.RegisteredNameForNick(args.SenderNickname) ?? args.SenderNickname)
                .ToLowerInvariant();
            string keywordLower = ((string)cmd.Arguments[0]).ToLowerInvariant();
            string message = ((string)cmd.Arguments[1]).Trim();

            using (var ctx = GetNewContext())
            {
                IKnewThatEntry entry = ctx.Entries
                    .FirstOrDefault(e => e.AuthorLowercase == senderLower && e.KeywordLowercase == keywordLower);

                if (entry == null)
                {
                    entry = new IKnewThatEntry
                    {
                        AuthorLowercase = senderLower,
                        KeywordLowercase = keywordLower,
                        Timestamp = DateTimeOffset.Now,
                        Message = message
                    };
                    ctx.Entries.Add(entry);
                    ctx.SaveChanges();

                    ConnectionManager.SendQueryMessage(
                        args.SenderNickname,
                        $"Okay, remembering that on {entry.Timestamp:yyyy-MM-dd} at {entry.Timestamp:HH:mm:ss}, you knew the following: {entry.Message}"
                    );
                }
                else
                {
                    DateTimeOffset oldTimestamp = entry.Timestamp.ToLocalTime();
                    string oldMessage = entry.Message;

                    entry.Timestamp = DateTimeOffset.Now;
                    entry.Message = message;
                    ctx.SaveChanges();

                    ConnectionManager.SendQueryMessage(
                        args.SenderNickname,
                        $"Okay, forgetting that on {oldTimestamp:yyyy-MM-dd} at {oldTimestamp:HH:mm:ss}, you knew the following: {oldMessage}; and remembering that on {entry.Timestamp:yyyy-MM-dd} at {entry.Timestamp:HH:mm:ss}, you knew the following: {entry.Message}"
                    );
                }
            }
        }

        private IKnewThatContext GetNewContext()
        {
            var opts = DatabaseUtil.GetContextOptions<IKnewThatContext>(Config);
            return new IKnewThatContext(opts);
        }
    }
}
