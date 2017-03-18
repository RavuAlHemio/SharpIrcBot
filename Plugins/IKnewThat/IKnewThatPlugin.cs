using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.IKnewThat.ORM;

namespace SharpIrcBot.Plugins.IKnewThat
{
    public class IKnewThatPlugin : IPlugin
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<IKnewThatPlugin>();
        public static readonly Regex IKnowThatRegex = new Regex("^!iknowthat\\s+(?<keyword>\\S+)\\s+(?<message>\\S+(?:\\s+\\S+)*)\\s*$", RegexOptions.Compiled);
        public static readonly Regex IKnewThatRegex = new Regex("^!iknewthat\\s+(?<keyword>\\S+)\\s*$", RegexOptions.Compiled);

        protected IKnewThatConfig Config { get; set; }
        protected IConnectionManager ConnectionManager;

        public IKnewThatPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new IKnewThatConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandleQueryMessage;
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            Match knewMatch = IKnewThatRegex.Match(args.Message);
            if (!knewMatch.Success)
            {
                return;
            }

            string senderLower = (ConnectionManager.RegisteredNameForNick(args.SenderNickname) ?? args.SenderNickname)
                .ToLowerInvariant();
            string keywordLower = knewMatch.Groups["keyword"].Value.ToLowerInvariant();

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

        protected virtual void HandleQueryMessage(object sender, IPrivateMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            Match knowMatch = IKnowThatRegex.Match(args.Message);
            if (!knowMatch.Success)
            {
                return;
            }

            string senderLower = (ConnectionManager.RegisteredNameForNick(args.SenderNickname) ?? args.SenderNickname)
                .ToLowerInvariant();
            string keywordLower = knowMatch.Groups["keyword"].Value.ToLowerInvariant();
            string message = knowMatch.Groups["message"].Value;

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
            var opts = SharpIrcBotUtil.GetContextOptions<IKnewThatContext>(Config);
            return new IKnewThatContext(opts);
        }
    }
}
