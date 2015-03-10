using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using Messenger.ORM;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Messenger
{
    /// <summary>
    /// Delivers messages to users when they return.
    /// </summary>
    public class MessengerPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Regex SendMessageRegex = new Regex("^!(s?)(?:msg|mail)[ ]+([^ ]+)[ ]+(.+)$");
        private static readonly Regex DeliverMessageRegex = new Regex("^!deliver(?:msg|mail) ([1-9][0-9]*)$");
        private static readonly Regex ReplayMessageRegex = new Regex("^!replay(?:msg|mail) ([1-9][0-9]*)$");
        private static readonly Regex IgnoreMessageRegex = new Regex("^!((?:un)?ignore)(?:msg|mail) ([^ ]+)$");

        private MessengerConfig _config;
        private IrcClient _client;

        public MessengerPlugin(IrcClient client, JObject config)
        {
            _client = client;
            _config = new MessengerConfig(config);

            _client.OnChannelMessage += HandleChannelMessage;
        }

        protected void PotentialMessageSend(IrcMessageData message)
        {
            var match = SendMessageRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            var rawRecipient = match.Groups[2].Value;
            var rawBody = match.Groups[3].Value;

            var lowerSender = message.Nick.ToLowerInvariant();
            var recipient = SharpIrcBotUtil.RemoveControlCharactersAndTrim(rawRecipient);
            var lowerRecipient = recipient.ToLowerInvariant();
            var body = SharpIrcBotUtil.RemoveControlCharactersAndTrim(rawBody);

            if (lowerRecipient.Length == 0)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format("{0}: You must specify a name to deliver to!", message.Nick));
                return;
            }
            if (body.Length == 0)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format("{0}: You must specify a message to deliver!", message.Nick));
                return;
            }
            if (lowerRecipient == _client.Nickname.ToLowerInvariant())
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format("{0}: Sorry, I don\u2019t deliver to myself!", message.Nick));
                return;
            }

            // check ignore list
            bool isIgnored;
            using (var ctx = GetNewContext())
            {
                isIgnored = ctx.IgnoreList.Any(il => il.SenderLowercase == lowerSender && il.RecipientLowercase == lowerRecipient);
            }

            if (isIgnored)
            {
                Logger.DebugFormat(
                    "{0} wants to send message {1} to {2}, but the recipient is ignoring the sender",
                    SharpIrcBotUtil.LiteralString(message.Nick),
                    SharpIrcBotUtil.LiteralString(body),
                    SharpIrcBotUtil.LiteralString(recipient)
                );
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: Can\u2019t send a message to {1}\u2014they\u2019re ignoring you.",
                    message.Nick,
                    recipient
                ));
                return;
            }

            Logger.DebugFormat(
                "{0} sending message {1} to {2}",
                SharpIrcBotUtil.LiteralString(message.Nick),
                SharpIrcBotUtil.LiteralString(body),
                SharpIrcBotUtil.LiteralString(recipient)
            );

            using (var ctx = GetNewContext())
            {
                var msg = new Message
                {
                    Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                    SenderOriginal = message.Nick,
                    RecipientLowercase = lowerRecipient,
                    Body = body
                };
                ctx.Messages.Add(msg);
                ctx.SaveChanges();
            }

            if (match.Groups[1].Value == "s")
            {
                // silent msg
                return;
            }

            if (lowerRecipient == lowerSender)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: Talking to ourselves? Well, no skin off my back. I\u2019ll deliver your message to you right away. ;)",
                    message.Nick
                ));
            }
            else
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: Aye-aye! I\u2019ll deliver your message to [i]{1}[/i] next time I see \u2019em!",
                    message.Nick,
                    recipient
                ));
            }
        }

        protected void PotentialDeliverRequest(IrcMessageData message)
        {
            var match = DeliverMessageRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            // overflow avoidance
            if (match.Groups[1].Length > 3)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: I am absolutely not delivering that many messages at once.",
                    message.Nick
                ));
                return;
            }
            var fetchCount = int.Parse(match.Groups[1].Value);
            var lowerSender = message.Nick.ToLowerInvariant();

            List<MessageOnRetainer> messages;
            int messagesLeft;
            using (var ctx = GetNewContext())
            {
                // get the messages
                messages = ctx.MessagesOnRetainer
                    .Where(m => m.RecipientLowercase == lowerSender)
                    .OrderBy(m => m.ID)
                    .Take(fetchCount)
                    .ToList()
                ;

                // delete them
                ctx.MessagesOnRetainer.RemoveRange(messages);
                ctx.SaveChanges();

                // check how many are left
                messagesLeft = ctx.MessagesOnRetainer
                    .Count(m => m.RecipientLowercase == lowerSender)
                ;
            }

            // deliver them
            if (messages.Count > 0)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "Delivering {0} {1} for {2}!",
                    messages.Count,
                    messages.Count == 1 ? "message" : "messages",
                    message.Nick
                ));
                foreach (var msg in messages)
                {
                    Logger.DebugFormat(
                        "delivering {0}'s retained message {1} to {2} as part of a chunk",
                        SharpIrcBotUtil.LiteralString(msg.SenderOriginal),
                        SharpIrcBotUtil.LiteralString(msg.Body),
                        SharpIrcBotUtil.LiteralString(message.Nick)
                    );
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "{0} <{1}> {2}",
                        FormatUtcTimestampFromDatabase(msg.Timestamp),
                        msg.SenderOriginal,
                        msg.Body
                    ));
                }
            }

            // output remaining messages count
            if (messagesLeft == 0)
            {
                if (messages.Count > 0)
                {
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "{0} has no more messages left to deliver!",
                        message.Nick
                    ));
                }
                else
                {
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "{0} has no messages to deliver!",
                        message.Nick
                    ));
                }
            }
            else
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0} has {1} {2} left to deliver!",
                    message.Nick,
                    messagesLeft,
                    (messagesLeft == 1) ? "message" : "messages"
                ));
            }
        }

        protected void PotentialReplayRequest(IrcMessageData message)
        {
            var match = ReplayMessageRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            if (match.Groups[1].Length > 3)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: I am absolutely not replaying that many messages at once.",
                    message.Nick
                ));
                return;
            }

            var replayCount = int.Parse(match.Groups[1].Value);
            if (replayCount > _config.MaxMessagesToReplay)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: I only remember a backlog of up to {1} messages.",
                    message.Nick,
                    _config.MaxMessagesToReplay
                ));
                return;
            }
            else if (replayCount == 0)
            {
                return;
            }

            var lowerSender = message.Nick.ToLowerInvariant();

            List<ReplayableMessage> messages;
            using (var ctx = GetNewContext())
            {
                // get the messages
                messages = ctx.ReplayableMessages
                    .Where(m => m.RecipientLowercase == lowerSender)
                    .OrderByDescending(m => m.ID)
                    .Take(replayCount)
                    .ToList()
                ;
                messages.Reverse();
            }

            if (messages.Count == 0)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: You have no messages to replay!",
                    message.Nick
                ));
                return;
            }
            if (messages.Count == 1)
            {
                Logger.DebugFormat("replaying a message for {0}", SharpIrcBotUtil.LiteralString(message.Nick));
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "Replaying message for {0}! {1} <{2}> {3}",
                    message.Nick,
                    FormatUtcTimestampFromDatabase(messages[0].Timestamp),
                    messages[0].SenderOriginal,
                    messages[0].Body
                ));
                return;
            }

            _client.SendMessage(SendType.Message, message.Channel, string.Format(
                "{0}: Replaying {1} messages!",
                message.Nick,
                messages.Count
            ));
            Logger.DebugFormat(
                "replaying {0} messages for {1}",
                messages.Count,
                SharpIrcBotUtil.LiteralString(message.Nick)
            );
            foreach (var msg in messages)
            {
                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0} <{1}> {2}!",
                    FormatUtcTimestampFromDatabase(msg.Timestamp),
                    msg.SenderOriginal,
                    msg.Body
                ));
            }
            _client.SendMessage(SendType.Message, message.Channel, string.Format(
                "{0}: Take care!",
                message.Nick
            ));
        }

        protected void PotentialIgnoreListRequest(IrcMessageData message)
        {
            var match = IgnoreMessageRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            var command = match.Groups[1].Value;
            var blockSender = match.Groups[2].Value.Trim();
            var blockSenderLower = blockSender.ToLowerInvariant();
            var blockRecipientLower = message.Nick.ToLowerInvariant();

            bool isIgnored;
            using (var ctx = GetNewContext())
            {
                isIgnored = ctx.IgnoreList
                    .Any(ie => ie.SenderLowercase == blockSenderLower && ie.RecipientLowercase == blockRecipientLower);
            }

            if (command == "ignore")
            {
                if (isIgnored)
                {
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "{0}: You are already ignoring [i]{1}[/i].",
                        message.Nick,
                        blockSender
                    ));
                    return;
                }

                using (var ctx = GetNewContext())
                {
                    var entry = new IgnoreEntry
                    {
                        SenderLowercase = blockSenderLower,
                        RecipientLowercase = blockRecipientLower
                    };
                    ctx.IgnoreList.Add(entry);
                    ctx.SaveChanges();
                }
                Logger.DebugFormat(
                    "{0} is now ignoring {1}",
                    SharpIrcBotUtil.LiteralString(message.Nick),
                    SharpIrcBotUtil.LiteralString(blockSender)
                );

                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: You are now ignoring [i]{1}[/i].",
                    message.Nick,
                    blockSender
                ));
            }
            else if (command == "unignore")
            {
                if (!isIgnored)
                {
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "{0}: You have not been ignoring [i]{1}[/i].",
                        message.Nick,
                        blockSender
                    ));
                    return;
                }

                using (var ctx = GetNewContext())
                {
                    var entry = ctx.IgnoreList
                        .FirstOrDefault(ie => ie.SenderLowercase == blockSenderLower && ie.RecipientLowercase == blockRecipientLower);
                    ctx.IgnoreList.Remove(entry);
                    ctx.SaveChanges();
                }
                Logger.DebugFormat(
                    "{0} is not ignoring {1} anymore",
                    SharpIrcBotUtil.LiteralString(message.Nick),
                    SharpIrcBotUtil.LiteralString(blockSender)
                );

                _client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: You are not ignoring [i]{1}[/i] anymore.",
                    message.Nick,
                    blockSender
                ));
            }
        }

        private MessengerContext GetNewContext()
        {
            var conn = SharpIrcBotUtil.GetDatabaseConnection(_config);
            return new MessengerContext(conn);
        }

        protected string FormatUtcTimestampFromDatabase(DateTime timestamp)
        {
            var localTime = timestamp.ToLocalTimeFromDatabase();
            return localTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected void ActuallyHandleChannelMessage(object sender, IrcEventArgs args)
        {
            var message = args.Data;
            if (message.Type != ReceiveType.ChannelMessage || message.Nick == _client.Nickname)
            {
                return;
            }

            var senderLower = message.Nick.ToLowerInvariant();

            PotentialMessageSend(message);
            PotentialDeliverRequest(message);
            PotentialIgnoreListRequest(message);
            PotentialReplayRequest(message);

            // even banned users get messages; they just can't respond to them

            // check if the sender should get any messages
            List<Message> messages;
            using (var ctx = GetNewContext())
            {
                messages = ctx.Messages
                    .Where(m => m.RecipientLowercase == senderLower)
                    .OrderBy(m => m.ID)
                    .ToList()
                ;
                var numberMessagesOnRetainer = ctx.MessagesOnRetainer
                    .Count(m => m.RecipientLowercase == senderLower);

                var retainerText = (numberMessagesOnRetainer > 0)
                    ? string.Format(" (and {0} pending !delivermsg)", numberMessagesOnRetainer)
                    : ""
                ;

                var moveToReplay = true;
                if (messages.Count == 0)
                {
                    // meh
                    // (don't return yet; delete the skipped "responded directly to" messages)
                }
                else if (messages.Count == 1)
                {
                    // one message
                    Logger.DebugFormat(
                        "delivering {0}'s message {1} to {2}",
                        SharpIrcBotUtil.LiteralString(messages[0].SenderOriginal),
                        SharpIrcBotUtil.LiteralString(messages[0].Body),
                        SharpIrcBotUtil.LiteralString(message.Nick)
                    );
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "Message for {0}{1}! {2} <{3}> {4}",
                        message.Nick,
                        retainerText,
                        FormatUtcTimestampFromDatabase(messages[0].Timestamp),
                        messages[0].SenderOriginal,
                        messages[0].Body
                    ));
                }
                else if (messages.Count >= _config.TooManyMessages)
                {
                    // use messages instead of messagesToDisplay to put all of them on retainer
                    Logger.DebugFormat(
                        "{0} got {1} messages; putting on retainer",
                        SharpIrcBotUtil.LiteralString(message.Nick),
                        messages.Count
                    );
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "{0} new messages for {1}{2}! Use \u201c!delivermsg [i]maxnumber[/i]\u201d to get them!",
                        messages.Count,
                        message.Nick,
                        retainerText
                    ));

                    // put messages on retainer
                    ctx.MessagesOnRetainer.AddRange(messages.Select(m => new MessageOnRetainer(m)));

                    // don't replay!
                    moveToReplay = false;

                    // the content of messages will be cleaned out from ctx.Messages below
                }
                else
                {
                    // multiple but not too many messages
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "{0} new messages for {1}{2}!",
                        messages.Count,
                        message.Nick,
                        retainerText
                    ));
                    foreach (var msg in messages)
                    {
                        Logger.DebugFormat(
                            "delivering {0}'s message {1} to {2} as part of a chunk",
                            SharpIrcBotUtil.LiteralString(msg.SenderOriginal),
                            SharpIrcBotUtil.LiteralString(msg.Body),
                            SharpIrcBotUtil.LiteralString(message.Nick)
                        );
                        _client.SendMessage(SendType.Message, message.Channel, string.Format(
                            "{0} <{1}> {2}",
                            FormatUtcTimestampFromDatabase(msg.Timestamp),
                            msg.SenderOriginal,
                            msg.Body
                        ));
                    }
                    _client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "{0}: Have a nice day!",
                        message.Nick
                    ));
                }

                if (moveToReplay)
                {
                    // place the messages on the repeat heap
                    ctx.ReplayableMessages.AddRange(messages.Select(
                        m => new ReplayableMessage(m)
                        {
                            Timestamp = m.Timestamp.ToUniversalTimeForDatabase()
                        }
                    ));
                }

                // purge the repeat heap if necessary
                var currentReplayables = ctx.ReplayableMessages
                    .Where(rm => rm.RecipientLowercase == senderLower)
                    .OrderBy(rm => rm.ID)
                    .ToList()
                ;
                if (currentReplayables.Count > _config.MaxMessagesToReplay)
                {
                    var deleteCount = currentReplayables.Count - _config.MaxMessagesToReplay;
                    foreach (var oldReplayable in currentReplayables.Take(deleteCount))
                    {
                        ctx.ReplayableMessages.Remove(oldReplayable);
                    }
                }

                // remove the messages from the delivery queue
                ctx.Messages.RemoveRange(messages);

                // commit
                ctx.SaveChanges();
            }
        }
    }
}
