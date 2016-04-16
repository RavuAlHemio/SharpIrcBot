using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using Messenger.ORM;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;

namespace Messenger
{
    /// <summary>
    /// Delivers messages to users when they return.
    /// </summary>
    public class MessengerPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly Regex SendMessageRegex = new Regex("^!(?<silence>s?)(?:msg|mail)\\s+(?<recipient>[^ :]+):?\\s+(?<message>\\S+(?:\\s+\\S+)*)\\s*$", RegexOptions.Compiled);
        public static readonly Regex DeliverMessageRegex = new Regex("^!deliver(?:msg|mail)\\s+(?<count>[1-9][0-9]*)\\s*$", RegexOptions.Compiled);
        public static readonly Regex ReplayMessageRegex = new Regex("^!replay(?:msg|mail)\\s+(?<count>[1-9][0-9]*)\\s*$", RegexOptions.Compiled);
        public static readonly Regex IgnoreMessageRegex = new Regex("^!(?<command>(?:un)?ignore)(?:msg|mail)\\s+(?<target>\\S+)\\s*$", RegexOptions.Compiled);
        public static readonly Regex QuiesceRegex = new Regex("^!(?:msg|mail)gone\\s+(?<messageCount>0|[1-9][0-9]*)\\s+(?<durationHours>[1-9][0-9]*)h\\s*$", RegexOptions.Compiled);
        public static readonly Regex UnQuiesceRegex = new Regex("^!(?:msg|mail)back\\s*$", RegexOptions.Compiled);

        protected MessengerConfig Config { get; set; }
        protected IConnectionManager ConnectionManager { get; set; }

        public MessengerPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new MessengerConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.BaseNickChanged += HandleBaseNickChanged;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new MessengerConfig(newConfig);
        }

        protected void PotentialMessageSend(IChannelMessageEventArgs message)
        {
            var match = SendMessageRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            string rawRecipientNickString = match.Groups["recipient"].Value;
            string[] rawRecipientNicks = rawRecipientNickString.Split(';');
            if (rawRecipientNicks.Length > 1 && !Config.AllowMulticast)
            {
                ConnectionManager.SendChannelMessageFormat(message.Channel, "{0}: Sorry, multicasting is not allowed!", message.SenderNickname);
                return;
            }

            var rawBody = match.Groups["message"].Value;
            var body = SharpIrcBotUtil.RemoveControlCharactersAndTrim(rawBody);

            var sender = ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname;
            var lowerSender = sender.ToLowerInvariant();

            if (body.Length == 0)
            {
                ConnectionManager.SendChannelMessageFormat(message.Channel, "{0}: You must specify a message to deliver!", message.SenderNickname);
                return;
            }

            IEnumerable<RecipientInfo> recipientEnumerable = rawRecipientNicks
                .Select(SharpIrcBotUtil.RemoveControlCharactersAndTrim)
                .Select(rn => new RecipientInfo(rn, ConnectionManager.RegisteredNameForNick(rn)));
            var recipients = new HashSet<RecipientInfo>(recipientEnumerable, new RecipientInfo.LowerRecipientComparer());

            foreach (var recipient in recipients)
            {
                if (recipient.LowerRecipient.Length == 0)
                {
                    ConnectionManager.SendChannelMessageFormat(message.Channel, "{0}: You must specify a name to deliver to!", message.SenderNickname);
                    return;
                }
                if (recipient.LowerRecipient == ConnectionManager.MyNickname.ToLowerInvariant())
                {
                    ConnectionManager.SendChannelMessageFormat(message.Channel, "{0}: Sorry, I don\u2019t deliver to myself!", message.SenderNickname);
                    return;
                }

                // check ignore list
                bool isIgnored;
                using (var ctx = GetNewContext())
                {
                    string lowerRecipient = recipient.LowerRecipient;
                    isIgnored = ctx.IgnoreList.Any(il => il.SenderLowercase == lowerSender && il.RecipientLowercase == lowerRecipient);
                }

                if (isIgnored)
                {
                    Logger.DebugFormat(
                        "{0} ({3}) wants to send message {1} to {2}, but the recipient is ignoring the sender",
                        SharpIrcBotUtil.LiteralString(message.SenderNickname),
                        SharpIrcBotUtil.LiteralString(body),
                        SharpIrcBotUtil.LiteralString(recipient.Recipient),
                        SharpIrcBotUtil.LiteralString(sender)
                    );
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: Can\u2019t send a message to {1}\u2014they\u2019re ignoring you.",
                        message.SenderNickname,
                        recipient.Recipient
                    );
                    return;
                }
            }

            Logger.DebugFormat(
                "{0} ({3}) sending message {1} to {2}",
                SharpIrcBotUtil.LiteralString(message.SenderNickname),
                SharpIrcBotUtil.LiteralString(body),
                string.Join(", ", recipients.Select(r => SharpIrcBotUtil.LiteralString(r.Recipient))),
                SharpIrcBotUtil.LiteralString(sender)
            );

            DateTimeOffset? quiescenceEnd = null;
            using (var ctx = GetNewContext())
            {
                foreach (var recipient in recipients)
                {
                    var msg = new Message
                    {
                        Timestamp = DateTimeOffset.Now,
                        SenderOriginal = message.SenderNickname,
                        RecipientLowercase = recipient.LowerRecipient,
                        Body = body
                    };
                    ctx.Messages.Add(msg);
                    ctx.SaveChanges();

                    // check for quiescence!
                    string lowerRecipient = recipient.LowerRecipient;
                    quiescenceEnd = ctx.Quiescences
                        .FirstOrDefault(q => q.UserLowercase == lowerRecipient)
                        ?.EndTimestamp;
                    if (quiescenceEnd.HasValue && quiescenceEnd.Value <= DateTimeOffset.Now)
                    {
                        quiescenceEnd = null;
                    }
                }
            }

            if (match.Groups["silence"].Value == "s")
            {
                // silent msg
                return;
            }

            if (recipients.Count > 1)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: Aye-aye! I\u2019ll deliver your message to its recipients as soon as possible!"
                );
                return;
            }

            var singleRecipient = recipients.First();
            if (singleRecipient.LowerRecipient == lowerSender)
            {
                if (quiescenceEnd.HasValue)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: Talking to ourselves? Well, no skin off my back. I\u2019ll deliver your message to you once I see you after {1}. ;)",
                        message.SenderNickname,
                        FormatUtcTimestampFromDatabase(quiescenceEnd.Value)
                    );
                }
                else
                {
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: Talking to ourselves? Well, no skin off my back. I\u2019ll deliver your message to you right away. ;)",
                        message.SenderNickname
                    );
                }
            }
            else
            {
                if (quiescenceEnd.HasValue)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: Aye-aye! I\u2019ll deliver your message to {1} next time I see \u2019em after {2}!",
                        message.SenderNickname,
                        singleRecipient.Recipient,
                        FormatUtcTimestampFromDatabase(quiescenceEnd.Value)
                    );
                }
                else
                {
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: Aye-aye! I\u2019ll deliver your message to {1} next time I see \u2019em!",
                        message.SenderNickname,
                        singleRecipient.Recipient
                    );
                }
            }
        }

        protected void PotentialDeliverRequest(IChannelMessageEventArgs message)
        {
            var match = DeliverMessageRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            // overflow avoidance
            if (match.Groups["count"].Length > 3)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: I am absolutely not delivering that many messages at once.",
                    message.SenderNickname
                );
                return;
            }
            var fetchCount = int.Parse(match.Groups["count"].Value);
            var sender = ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname;
            var lowerSender = sender.ToLowerInvariant();

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
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "Delivering {0} {1} for {2}!",
                    messages.Count,
                    messages.Count == 1 ? "message" : "messages",
                    message.SenderNickname
                );
                foreach (var msg in messages)
                {
                    Logger.DebugFormat(
                        "delivering {0}'s retained message {1} to {2} as part of a chunk",
                        SharpIrcBotUtil.LiteralString(msg.SenderOriginal),
                        SharpIrcBotUtil.LiteralString(msg.Body),
                        SharpIrcBotUtil.LiteralString(message.SenderNickname)
                    );
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0} <{1}> {2}",
                        FormatUtcTimestampFromDatabase(msg.Timestamp),
                        msg.SenderOriginal,
                        msg.Body
                    );
                }
            }

            // output remaining messages count
            if (messagesLeft == 0)
            {
                if (messages.Count > 0)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0} has no more messages left to deliver!",
                        message.SenderNickname
                    );
                }
                else
                {
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0} has no messages to deliver!",
                        message.SenderNickname
                    );
                }
            }
            else
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0} has {1} {2} left to deliver!",
                    message.SenderNickname,
                    messagesLeft,
                    (messagesLeft == 1) ? "message" : "messages"
                );
            }
        }

        protected void PotentialReplayRequest(IChannelMessageEventArgs message)
        {
            var match = ReplayMessageRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            if (match.Groups["count"].Length > 3)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: I am absolutely not replaying that many messages at once.",
                    message.SenderNickname
                );
                return;
            }

            var replayCount = int.Parse(match.Groups["count"].Value);
            if (replayCount > Config.MaxMessagesToReplay)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: I only remember a backlog of up to {1} messages.",
                    message.SenderNickname,
                    Config.MaxMessagesToReplay
                );
                return;
            }
            else if (replayCount == 0)
            {
                return;
            }

            var sender = ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname;
            var lowerSender = sender.ToLowerInvariant();

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
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: You have no messages to replay!",
                    message.SenderNickname
                );
                return;
            }
            if (messages.Count == 1)
            {
                Logger.DebugFormat("replaying a message for {0}", SharpIrcBotUtil.LiteralString(message.SenderNickname));
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "Replaying message for {0}! {1} <{2}> {3}",
                    message.SenderNickname,
                    FormatUtcTimestampFromDatabase(messages[0].Timestamp),
                    messages[0].SenderOriginal,
                    messages[0].Body
                );
                return;
            }

            ConnectionManager.SendChannelMessageFormat(
                message.Channel,
                "{0}: Replaying {1} messages!",
                message.SenderNickname,
                messages.Count
            );
            Logger.DebugFormat(
                "replaying {0} messages for {1}",
                messages.Count,
                SharpIrcBotUtil.LiteralString(message.SenderNickname)
            );
            foreach (var msg in messages)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0} <{1}> {2}",
                    FormatUtcTimestampFromDatabase(msg.Timestamp),
                    msg.SenderOriginal,
                    msg.Body
                );
            }
            ConnectionManager.SendChannelMessageFormat(
                message.Channel,
                "{0}: Take care!",
                message.SenderNickname
            );
        }

        protected void PotentialIgnoreListRequest(IChannelMessageEventArgs message)
        {
            var match = IgnoreMessageRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            var command = match.Groups["command"].Value;
            var blockSenderNickname = match.Groups["target"].Value.Trim();
            var blockSender = ConnectionManager.RegisteredNameForNick(blockSenderNickname) ?? blockSenderNickname;
            var blockSenderLower = blockSender.ToLowerInvariant();
            var blockRecipient = ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname;
            var blockRecipientLower = blockRecipient.ToLowerInvariant();

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
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: You are already ignoring {1}.",
                        message.SenderNickname,
                        blockSender
                    );
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
                    "{0} ({2}) is now ignoring {1}",
                    SharpIrcBotUtil.LiteralString(message.SenderNickname),
                    SharpIrcBotUtil.LiteralString(blockSender),
                    SharpIrcBotUtil.LiteralString(blockRecipient)
                );

                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: You are now ignoring {1}.",
                    message.SenderNickname,
                    blockSender
                );
            }
            else if (command == "unignore")
            {
                if (!isIgnored)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: You have not been ignoring {1}.",
                        message.SenderNickname,
                        blockSender
                    );
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
                    "{0} ({2}) is not ignoring {1} anymore",
                    SharpIrcBotUtil.LiteralString(message.SenderNickname),
                    SharpIrcBotUtil.LiteralString(blockSender),
                    SharpIrcBotUtil.LiteralString(blockRecipient)
                );

                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: You are not ignoring {1} anymore.",
                    message.SenderNickname,
                    blockSender
                );
            }
        }

        protected void PotentialQuiesceRequest(IChannelMessageEventArgs message)
        {
            var match = QuiesceRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            int lastMessageCount, hoursToSkip;
            if (!int.TryParse(match.Groups["messageCount"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out lastMessageCount))
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: That\u2019s way too many messages.",
                    message.SenderNickname
                );
                return;
            }

            const string tooManyHoursFormat = "{0}: I seriously doubt you\u2019ll live that long...";
            if (!int.TryParse(match.Groups["durationHours"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out hoursToSkip))
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    tooManyHoursFormat,
                    message.SenderNickname
                );
                return;
            }

            var quiesceUserLowercase = (ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname).ToLowerInvariant();

            // calculate end time
            DateTimeOffset endTime;
            try
            {
                endTime = DateTimeOffset.Now.AddHours(hoursToSkip);
            }
            catch (ArgumentOutOfRangeException)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    tooManyHoursFormat,
                    message.SenderNickname
                );
                return;
            }

            int? tooFewCount = null;
            using (var ctx = GetNewContext())
            {
                // find existing quiescence item and remove it if necessary
                var existingQuiescence = ctx.Quiescences.FirstOrDefault(q => q.UserLowercase == quiesceUserLowercase);
                if (existingQuiescence != null)
                {
                    ctx.Quiescences.Remove(existingQuiescence);
                }

                // add quiescence item
                ctx.Quiescences.Add(new Quiescence
                {
                    UserLowercase = quiesceUserLowercase,
                    EndTimestamp = endTime
                });
                ctx.SaveChanges();

                if (lastMessageCount > 0)
                {
                    // shunt chosen number of messages from replayable back to regular
                    List<ReplayableMessage> replayables = ctx.ReplayableMessages
                        .Where(m => m.RecipientLowercase == quiesceUserLowercase)
                        .OrderByDescending(m => m.ID)
                        .Take(lastMessageCount)
                        .ToList();
                    if (replayables.Count < lastMessageCount)
                    {
                        tooFewCount = replayables.Count;
                    }
                    ctx.Messages.AddRange(replayables.Select(rm => new Message(rm)));
                    ctx.ReplayableMessages.RemoveRange(replayables);
                    ctx.SaveChanges();
                }
            }

            if (lastMessageCount == 0)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: Okay, I won\u2019t bug you until {1}.",
                    message.SenderNickname,
                    FormatUtcTimestampFromDatabase(endTime)
                );
            }
            else if (tooFewCount.HasValue)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: Okay, I won\u2019t bug you until {1}, but I only remembered and requeued the last {2} messages...",
                    message.SenderNickname,
                    FormatUtcTimestampFromDatabase(endTime),
                    tooFewCount.Value
                );
            }
            else
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: Okay, I won\u2019t bug you until {1}, and I requeued your last {2} messages.",
                    message.SenderNickname,
                    FormatUtcTimestampFromDatabase(endTime),
                    lastMessageCount
                );
            }
        }

        protected void PotentialUnquiesceRequest(IChannelMessageEventArgs message)
        {
            var match = UnQuiesceRegex.Match(message.Message);
            if (!match.Success)
            {
                return;
            }

            var unquiesceUserLowercase = (ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname).ToLowerInvariant();
            using (var ctx = GetNewContext())
            {
                var quiescence = ctx.Quiescences
                    .FirstOrDefault(q => q.UserLowercase == unquiesceUserLowercase);
                if (quiescence == null)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: You never really were gone...",
                        message.SenderNickname
                    );
                    return;
                }

                ctx.Quiescences.Remove(quiescence);
                ctx.SaveChanges();
            }

            ConnectionManager.SendChannelMessageFormat(
                message.Channel,
                "{0}: Welcome back!",
                message.SenderNickname
            );
        }

        private MessengerContext GetNewContext()
        {
            var conn = SharpIrcBotUtil.GetDatabaseConnection(Config);
            return new MessengerContext(conn);
        }

        protected string FormatUtcTimestampFromDatabase(DateTimeOffset timestamp)
        {
            var localTime = timestamp.ToLocalTime();
            return localTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        protected void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (args.SenderNickname == ConnectionManager.MyNickname)
            {
                return;
            }

            var senderUser = ConnectionManager.RegisteredNameForNick(args.SenderNickname) ?? args.SenderNickname;
            var senderLower = senderUser.ToLowerInvariant();

            if (!flags.HasFlag(MessageFlags.UserBanned))
            {
                PotentialMessageSend(args);
                PotentialReplayRequest(args);
                PotentialIgnoreListRequest(args);
                PotentialQuiesceRequest(args);
                PotentialUnquiesceRequest(args);
            }

            PotentialDeliverRequest(args);

            // even banned users get messages; they just can't respond to them

            // only deliver if we are in a delivery channel
            if (Config.DeliveryChannels.Count > 0 && !Config.DeliveryChannels.Contains(args.Channel))
            {
                return;
            }

            // check if the sender should get any messages
            List<Message> messages;
            using (var ctx = GetNewContext())
            {
                var quiescence = ctx.Quiescences
                    .FirstOrDefault(q => q.UserLowercase == senderLower);
                if (quiescence != null)
                {
                    if (quiescence.EndTimestamp > DateTimeOffset.Now)
                    {
                        // active quiescence; don't deliver yet
                        return;
                    }
                    else
                    {
                        // delete this quiescence; it is outdated
                        ctx.Quiescences.Remove(quiescence);
                        ctx.SaveChanges();
                    }
                }

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
                        SharpIrcBotUtil.LiteralString(args.SenderNickname)
                    );
                    ConnectionManager.SendChannelMessageFormat(
                        args.Channel,
                        "Message for {0}{1}! {2} <{3}> {4}",
                        args.SenderNickname,
                        retainerText,
                        FormatUtcTimestampFromDatabase(messages[0].Timestamp),
                        messages[0].SenderOriginal,
                        messages[0].Body
                    );
                }
                else if (messages.Count >= Config.TooManyMessages)
                {
                    // use messages instead of messagesToDisplay to put all of them on retainer
                    Logger.DebugFormat(
                        "{0} got {1} messages; putting on retainer",
                        SharpIrcBotUtil.LiteralString(args.SenderNickname),
                        messages.Count
                    );
                    ConnectionManager.SendChannelMessageFormat(
                        args.Channel,
                        "{0} new messages for {1}{2}! Use \u201c!delivermsg maxnumber\u201d to get them!",
                        messages.Count,
                        args.SenderNickname,
                        retainerText
                    );

                    // put messages on retainer
                    ctx.MessagesOnRetainer.AddRange(messages.Select(m => new MessageOnRetainer(m)));

                    // don't replay!
                    moveToReplay = false;

                    // the content of messages will be cleaned out from ctx.Messages below
                }
                else
                {
                    // multiple but not too many messages
                    ConnectionManager.SendChannelMessageFormat(
                        args.Channel,
                        "{0} new messages for {1}{2}!",
                        messages.Count,
                        args.SenderNickname,
                        retainerText
                    );
                    foreach (var msg in messages)
                    {
                        Logger.DebugFormat(
                            "delivering {0}'s message {1} to {2} as part of a chunk",
                            SharpIrcBotUtil.LiteralString(msg.SenderOriginal),
                            SharpIrcBotUtil.LiteralString(msg.Body),
                            SharpIrcBotUtil.LiteralString(args.SenderNickname)
                        );
                        ConnectionManager.SendChannelMessageFormat(
                            args.Channel,
                            "{0} <{1}> {2}",
                            FormatUtcTimestampFromDatabase(msg.Timestamp),
                            msg.SenderOriginal,
                            msg.Body
                        );
                    }
                    ConnectionManager.SendChannelMessageFormat(
                        args.Channel,
                        "{0}: Have a nice day!",
                        args.SenderNickname
                    );
                }

                if (moveToReplay)
                {
                    // place the messages on the repeat heap
                    ctx.ReplayableMessages.AddRange(messages.Select(
                        m => new ReplayableMessage(m)
                        {
                            Timestamp = m.Timestamp
                        }
                    ));
                }

                // purge the repeat heap if necessary
                var currentReplayables = ctx.ReplayableMessages
                    .Where(rm => rm.RecipientLowercase == senderLower)
                    .OrderBy(rm => rm.ID)
                    .ToList()
                ;
                if (currentReplayables.Count > Config.MaxMessagesToReplay)
                {
                    var deleteCount = currentReplayables.Count - Config.MaxMessagesToReplay;
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

        protected virtual void ReassignMessages(IQueryable<IMessage> messages, string oldNick, string newNick)
        {
            var lowerOldNick = oldNick.ToLowerInvariant();
            var lowerNewNick = newNick.ToLowerInvariant();

            var sentMessages = messages
                .Where(m => m.SenderOriginal.ToLower() == lowerOldNick);
            foreach (var sentMessage in sentMessages)
            {
                sentMessage.SenderOriginal = newNick;
            }

            var receivedMessages = messages
                .Where(m => m.RecipientLowercase == lowerOldNick);
            foreach (var receivedMessage in receivedMessages)
            {
                receivedMessage.RecipientLowercase = lowerNewNick;
            }
        }

        protected virtual void HandleBaseNickChanged(object sender, BaseNickChangedEventArgs e)
        {
            using (var ctx = GetNewContext())
            {
                // reassign all messages from the old to the new nick
                ReassignMessages(ctx.Messages, e.OldBaseNick, e.NewBaseNick);
                ReassignMessages(ctx.ReplayableMessages, e.OldBaseNick, e.NewBaseNick);
                ReassignMessages(ctx.MessagesOnRetainer, e.OldBaseNick, e.NewBaseNick);
                ctx.SaveChanges();

                var lowerOldNick = e.OldBaseNick.ToLowerInvariant();
                var lowerNewNick = e.NewBaseNick.ToLowerInvariant();

                // update ignore lists
                var ignorances = ctx.IgnoreList
                    .Where(ie => ie.SenderLowercase == lowerOldNick || ie.RecipientLowercase == lowerOldNick);
                foreach (var ignorance in ignorances)
                {
                    if (ignorance.SenderLowercase == lowerOldNick)
                    {
                        ignorance.SenderLowercase = lowerNewNick;
                    }
                    else if (ignorance.RecipientLowercase == lowerOldNick)
                    {
                        ignorance.RecipientLowercase = lowerNewNick;
                    }

                    // delete if this became a self-ignorance
                    if (ignorance.SenderLowercase == ignorance.RecipientLowercase)
                    {
                        ctx.IgnoreList.Remove(ignorance);
                    }
                }
                ctx.SaveChanges();

                // update quiescences
                var quiescences = ctx.Quiescences
                    .Where(q => q.UserLowercase == lowerOldNick);
                foreach (var quiescence in quiescences)
                {
                    quiescence.UserLowercase = lowerNewNick;
                }
                ctx.SaveChanges();
            }
        }
    }
}
