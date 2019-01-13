using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Messenger.ORM;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Messenger
{
    /// <summary>
    /// Delivers messages to users when they return.
    /// </summary>
    public class MessengerPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<MessengerPlugin>();

        protected MessengerConfig Config { get; set; }
        protected IConnectionManager ConnectionManager { get; set; }

        public MessengerPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new MessengerConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.ChannelAction += HandleChannelAction;
            ConnectionManager.BaseNickChanged += HandleBaseNickChanged;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("msg", "mail", "smsg", "smail"),
                    CommandUtil.MakeOptions(
                        CommandUtil.MakeFlag("-x"), CommandUtil.MakeFlag("--exact-nickname")
                    ),
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // recipient
                        RestTaker.Instance // message
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleMsgCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("delivermsg", "delivermail"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        new LongMatcher().ToRequiredWordTaker() // count
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleDeliverCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("replaymsg", "replaymail"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        new LongMatcher().ToRequiredWordTaker() // count
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleReplayCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("ignoremsg", "ignoremail", "unignoremsg", "unignoremail"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // sender
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleIgnoreCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("msggone", "mailgone"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        new LongMatcher().ToRequiredWordTaker(), // number of messages to redeliver eventually
                        new RegexMatcher("^(?<hours>[1-9][0-9]*)h$").ToRequiredWordTaker() // duration
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleGoneCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("msgback", "mailback"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleBackCommand
            );
            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("pm", "pmsg", "pmail"),
                    CommandUtil.MakeOptions(
                        CommandUtil.MakeFlag("-x"), CommandUtil.MakeFlag("--exact-nickname")
                    ),
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // recipient
                        RestTaker.Instance // message
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandlePrivateMessageCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new MessengerConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected void HandleMsgCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            string rawRecipientNickString = ((string)cmd.Arguments[0]).TrimEnd(':', ',');
            string[] rawRecipientNicks = rawRecipientNickString.Split(';');
            if (rawRecipientNicks.Length > 1 && !Config.AllowMulticast)
            {
                ConnectionManager.SendChannelMessageFormat(message.Channel, "{0}: Sorry, multicasting is not allowed!", message.SenderNickname);
                return;
            }

            bool exactNickname = cmd.Options.Any(o => o.Key == "-x" || o.Key == "--exact-nickname");
            var rawBody = (string)cmd.Arguments[1];
            var body = StringUtil.RemoveControlCharactersAndTrim(rawBody);

            string senderUser = ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname;
            string lowerSenderUser = senderUser.ToLowerInvariant();

            if (body.Length == 0)
            {
                ConnectionManager.SendChannelMessageFormat(message.Channel, "{0}: You must specify a message to deliver!", message.SenderNickname);
                return;
            }

            IEnumerable<RecipientInfo> recipientEnumerable = rawRecipientNicks
                .Select(StringUtil.RemoveControlCharactersAndTrim)
                .Select(rn => new RecipientInfo(rn, ConnectionManager.RegisteredNameForNick(rn), exactNickname));
            var recipients = new HashSet<RecipientInfo>(recipientEnumerable, RecipientInfo.LowerRecipientComparer.Instance);

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

                // validate nickname
                if (recipient.RecipientUser == null && !ConnectionManager.IsValidNickname(recipient.RecipientNick))
                {
                    Logger.LogDebug(
                        "{SenderNickname} ({SenderUsername}) wants to send message {Message} to {RecipientNick}, but that's an invalid nickname and no alias was found",
                        StringUtil.LiteralString(message.SenderNickname),
                        StringUtil.LiteralString(senderUser),
                        StringUtil.LiteralString(body),
                        StringUtil.LiteralString(recipient.RecipientNick)
                    );
                    ConnectionManager.SendChannelMessageFormat(
                        message.Channel,
                        "{0}: Can\u2019t send a message to {1}\u2014that\u2019s neither a valid nickname nor a known alias.",
                        message.SenderNickname,
                        recipient.Recipient
                    );
                }

                // check ignore list
                bool isIgnored;
                using (var ctx = GetNewContext())
                {
                    string lowerRecipientUser = recipient.LowerRecipientUser;
                    isIgnored = ctx.IgnoreList.Any(il =>
                        il.SenderLowercase == lowerSenderUser && il.RecipientLowercase == lowerRecipientUser
                    );
                }

                if (isIgnored)
                {
                    Logger.LogDebug(
                        "{SenderNickname} ({SenderUsername}) wants to send message {Message} to {Recipient}, but the recipient is ignoring the sender",
                        StringUtil.LiteralString(message.SenderNickname),
                        StringUtil.LiteralString(senderUser),
                        StringUtil.LiteralString(body),
                        StringUtil.LiteralString(recipient.Recipient)
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

            Logger.LogDebug(
                "{SenderNickname} ({SenderUsername}) sending message {Message} to {Recipients}",
                StringUtil.LiteralString(message.SenderNickname),
                StringUtil.LiteralString(senderUser),
                StringUtil.LiteralString(body),
                recipients.Select(r => StringUtil.LiteralString(r.Recipient)).StringJoin(", ")
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
                        Body = body,
                        ExactNickname = recipient.ExactNickname
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

            if (cmd.CommandName[0] == 's')
            {
                // silent msg
                return;
            }

            if (recipients.Count > 1)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: Aye-aye! I\u2019ll deliver your message to its recipients as soon as possible!",
                    message.SenderNickname
                );
                return;
            }

            var singleRecipient = recipients.First();
            if (singleRecipient.LowerRecipientUser == lowerSenderUser)
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

        protected void HandleDeliverCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            var longFetchCount = (long)cmd.Arguments[0];

            // overflow avoidance
            if (longFetchCount > 999)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: I am absolutely not delivering that many messages at once.",
                    message.SenderNickname
                );
                return;
            }
            var fetchCount = (int)longFetchCount;
            string senderUser = ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname;
            string lowerSenderUser = senderUser.ToLowerInvariant();
            string lowerSenderNick = message.SenderNickname.ToLowerInvariant();

            List<MessageOnRetainer> messages;
            int messagesLeft;
            using (var ctx = GetNewContext())
            {
                // get the messages
                messages = ctx.MessagesOnRetainer
                    .Where(GetMessageSelector<MessageOnRetainer>(lowerSenderNick, lowerSenderUser))
                    .OrderBy(m => m.ID)
                    .Take(fetchCount)
                    .ToList()
                ;

                // delete them
                ctx.MessagesOnRetainer.RemoveRange(messages);
                ctx.SaveChanges();

                // check how many are left
                messagesLeft = ctx.MessagesOnRetainer
                    .Count(GetMessageSelector<MessageOnRetainer>(lowerSenderNick, lowerSenderUser))
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
                    Logger.LogDebug(
                        "delivering {Sender}'s retained message {Message} to {Recipient} as part of a chunk",
                        StringUtil.LiteralString(msg.SenderOriginal),
                        StringUtil.LiteralString(msg.Body),
                        StringUtil.LiteralString(message.SenderNickname)
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

        protected void HandleReplayCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            var longReplayCount = (long)cmd.Arguments[0];

            // overflow avoidance
            if (longReplayCount > 999)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: I am absolutely not replaying that many messages at once.",
                    message.SenderNickname
                );
                return;
            }
            var replayCount = (int)longReplayCount;

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

            string senderUser = ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname;
            string lowerSenderUser = senderUser.ToLowerInvariant();
            string lowerSenderNick = message.SenderNickname.ToLowerInvariant();

            List<ReplayableMessage> messages;
            using (var ctx = GetNewContext())
            {
                // get the messages
                messages = ctx.ReplayableMessages
                    .Where(GetMessageSelector<ReplayableMessage>(lowerSenderNick, lowerSenderUser))
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
                Logger.LogDebug("replaying a message for {Recipient}", StringUtil.LiteralString(message.SenderNickname));
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
            Logger.LogDebug(
                "replaying {Count} messages for {Recipient}",
                messages.Count,
                StringUtil.LiteralString(message.SenderNickname)
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

        protected void HandleIgnoreCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            var blockSenderNickname = (string)cmd.Arguments[0];
            string blockSender = ConnectionManager.RegisteredNameForNick(blockSenderNickname) ?? blockSenderNickname;
            string blockSenderLower = blockSender.ToLowerInvariant();
            string blockRecipient = ConnectionManager.RegisteredNameForNick(message.SenderNickname)
                    ?? message.SenderNickname;
            string blockRecipientLower = blockRecipient.ToLowerInvariant();

            bool isIgnored;
            using (var ctx = GetNewContext())
            {
                isIgnored = ctx.IgnoreList
                    .Any(ie => ie.SenderLowercase == blockSenderLower && ie.RecipientLowercase == blockRecipientLower);
            }

            if (cmd.CommandName.StartsWith("ignore"))
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
                Logger.LogDebug(
                    "{BlockingRecipientNickname} ({BlockingRecipientUsername}) is now ignoring {BlockedSender}",
                    StringUtil.LiteralString(message.SenderNickname),
                    StringUtil.LiteralString(blockRecipient),
                    StringUtil.LiteralString(blockSender)
                );

                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: You are now ignoring {1}.",
                    message.SenderNickname,
                    blockSender
                );
            }
            else if (cmd.CommandName.StartsWith("unignore"))
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
                Logger.LogDebug(
                    "{BlockingRecipientNickname} ({BlockingRecipientUsername}) is not ignoring {BlockedSender} anymore",
                    StringUtil.LiteralString(message.SenderNickname),
                    StringUtil.LiteralString(blockRecipient),
                    StringUtil.LiteralString(blockSender)
                );

                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: You are not ignoring {1} anymore.",
                    message.SenderNickname,
                    blockSender
                );
            }
        }

        protected void HandleGoneCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            var longLastMessageCount = (long)cmd.Arguments[0];
            if (longLastMessageCount > int.MaxValue)
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: That\u2019s way too many messages.",
                    message.SenderNickname
                );
                return;
            }
            var lastMessageCount = (int)longLastMessageCount;

            int hoursToSkip;
            const string tooManyHoursFormat = "{0}: I seriously doubt you\u2019ll live that long...";
            var hoursMatch = (Match)cmd.Arguments[1];
            if (!int.TryParse(hoursMatch.Groups["hours"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out hoursToSkip))
            {
                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    tooManyHoursFormat,
                    message.SenderNickname
                );
                return;
            }

            var quiesceUserLowercase =
                    (ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname)
                    .ToLowerInvariant();

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

        protected void HandleBackCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            string unquiesceUserLowercase =
                    (ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname)
                    .ToLowerInvariant();
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

        protected void HandlePrivateMessageCommand(CommandMatch cmd, IPrivateMessageEventArgs message)
        {
            string rawRecipientNickString = ((string)cmd.Arguments[0]).TrimEnd(':', ',');
            string[] rawRecipientNicks = rawRecipientNickString.Split(';');
            if (rawRecipientNicks.Length > 1 && !Config.AllowMulticast)
            {
                ConnectionManager.SendQueryMessage(message.SenderNickname, "Sorry, multicasting is not allowed!");
                return;
            }

            bool exactNickname = cmd.Options.Any(o => o.Key == "-x" || o.Key == "--exact-nickname");
            var rawBody = (string)cmd.Arguments[1];
            var body = StringUtil.RemoveControlCharactersAndTrim(rawBody);

            string senderUser = ConnectionManager.RegisteredNameForNick(message.SenderNickname) ?? message.SenderNickname;
            string lowerSenderUser = senderUser.ToLowerInvariant();

            if (body.Length == 0)
            {
                ConnectionManager.SendQueryMessage(message.SenderNickname, "You must specify a message to deliver!");
                return;
            }

            IEnumerable<RecipientInfo> recipientEnumerable = rawRecipientNicks
                .Select(StringUtil.RemoveControlCharactersAndTrim)
                .Select(rn => new RecipientInfo(rn, ConnectionManager.RegisteredNameForNick(rn), exactNickname));
            var recipients = new HashSet<RecipientInfo>(recipientEnumerable, RecipientInfo.LowerRecipientComparer.Instance);

            foreach (var recipient in recipients)
            {
                if (recipient.LowerRecipient.Length == 0)
                {
                    ConnectionManager.SendQueryMessage(message.SenderNickname, "You must specify a name to deliver to!");
                    return;
                }
                if (recipient.LowerRecipient == ConnectionManager.MyNickname.ToLowerInvariant())
                {
                    ConnectionManager.SendQueryMessage(message.SenderNickname, "Sorry, I don\u2019t deliver to myself!");
                    return;
                }

                // validate nickname
                if (recipient.RecipientUser == null && !ConnectionManager.IsValidNickname(recipient.RecipientNick))
                {
                    Logger.LogDebug(
                        "{SenderNickname} ({SenderUsername}) wants to send private message {Message} to {RecipientNick}, but that's an invalid nickname and no alias was found",
                        StringUtil.LiteralString(message.SenderNickname),
                        StringUtil.LiteralString(senderUser),
                        StringUtil.LiteralString(body),
                        StringUtil.LiteralString(recipient.RecipientNick)
                    );
                    ConnectionManager.SendQueryMessageFormat(
                        message.SenderNickname,
                        "Can\u2019t send a message to {0}\u2014that\u2019s neither a valid nickname nor a known alias.",
                        recipient.Recipient
                    );
                }

                // check ignore list
                bool isIgnored;
                using (var ctx = GetNewContext())
                {
                    string lowerRecipientUser = recipient.LowerRecipientUser;
                    isIgnored = ctx.IgnoreList.Any(il =>
                        il.SenderLowercase == lowerSenderUser && il.RecipientLowercase == lowerRecipientUser
                    );
                }

                if (isIgnored)
                {
                    Logger.LogDebug(
                        "{SenderNickname} ({SenderUsername}) wants to send private message {Message} to {Recipient}, but the recipient is ignoring the sender",
                        StringUtil.LiteralString(message.SenderNickname),
                        StringUtil.LiteralString(senderUser),
                        StringUtil.LiteralString(body),
                        StringUtil.LiteralString(recipient.Recipient)
                    );
                    ConnectionManager.SendQueryMessageFormat(
                        message.SenderNickname,
                        "Can\u2019t send a PM to {0}\u2014they\u2019re ignoring you.",
                        recipient.Recipient
                    );
                    return;
                }
            }

            Logger.LogDebug(
                "{SenderNickname} ({SenderUsername}) sending private message {Message} to {Recipients}",
                StringUtil.LiteralString(message.SenderNickname),
                StringUtil.LiteralString(senderUser),
                StringUtil.LiteralString(body),
                recipients.Select(r => StringUtil.LiteralString(r.Recipient)).StringJoin(", ")
            );

            using (var ctx = GetNewContext())
            {
                foreach (var recipient in recipients)
                {
                    var msg = new PrivateMessage
                    {
                        Timestamp = DateTimeOffset.Now,
                        SenderOriginal = message.SenderNickname,
                        RecipientLowercase = recipient.LowerRecipient,
                        Body = body,
                        ExactNickname = recipient.ExactNickname
                    };
                    ctx.PrivateMessages.Add(msg);
                    ctx.SaveChanges();
                }
            }

            if (recipients.Count > 1)
            {
                ConnectionManager.SendQueryMessage(
                    message.SenderNickname,
                    "Aye-aye! I\u2019ll deliver your PM to its recipients as soon as possible!"
                );
                return;
            }

            var singleRecipient = recipients.First();
            if (singleRecipient.LowerRecipient == lowerSenderUser)
            {
                ConnectionManager.SendQueryMessage(
                    message.SenderNickname,
                    "Talking to ourselves? Well, no skin off my back. I\u2019ll deliver your PM to you right away. ;)"
                );
            }
            else
            {
                ConnectionManager.SendQueryMessageFormat(
                    message.SenderNickname,
                    "Aye-aye! I\u2019ll deliver your PM to {0} next time I see \u2019em!",
                    singleRecipient.Recipient
                );
            }
        }

        private MessengerContext GetNewContext()
        {
            var opts = DatabaseUtil.GetContextOptions<MessengerContext>(Config);
            return new MessengerContext(opts);
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

            // even banned users get messages; they just can't respond to them
            RegularMessageDelivery(args, senderLower);
            RegularPrivateMessageDelivery(args, senderLower);
        }

        protected void HandleChannelAction(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            var senderUser = ConnectionManager.RegisteredNameForNick(args.SenderNickname) ?? args.SenderNickname;
            var senderLower = senderUser.ToLowerInvariant();
            RegularMessageDelivery(args, senderLower);
            RegularPrivateMessageDelivery(args, senderLower);
        }

        protected virtual void RegularMessageDelivery(IChannelMessageEventArgs args, string senderUserLower)
        {
            // only deliver if we are in a delivery channel
            if (Config.DeliveryChannels.Count > 0 && !Config.DeliveryChannels.Contains(args.Channel))
            {
                return;
            }

            string senderNickLower = args.SenderNickname.ToLowerInvariant();

            // check if the sender should get any messages
            List<Message> messages;
            using (var ctx = GetNewContext())
            {
                var quiescence = ctx.Quiescences
                    .FirstOrDefault(q => q.UserLowercase == senderUserLower);
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
                    .Where(GetMessageSelector<Message>(senderNickLower, senderUserLower))
                    .OrderBy(m => m.ID)
                    .ToList()
                ;
                var numberMessagesOnRetainer = ctx.MessagesOnRetainer
                    .Count(GetMessageSelector<MessageOnRetainer>(senderNickLower, senderUserLower));

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
                    Logger.LogDebug(
                        "delivering {Sender}'s message {Message} to {Recipient}",
                        StringUtil.LiteralString(messages[0].SenderOriginal),
                        StringUtil.LiteralString(messages[0].Body),
                        StringUtil.LiteralString(args.SenderNickname)
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
                    Logger.LogDebug(
                        "{Recipient} got {Count} messages; putting on retainer",
                        StringUtil.LiteralString(args.SenderNickname),
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
                        Logger.LogDebug(
                            "delivering {Sender}'s message {Message} to {Recipient} as part of a chunk",
                            StringUtil.LiteralString(msg.SenderOriginal),
                            StringUtil.LiteralString(msg.Body),
                            StringUtil.LiteralString(args.SenderNickname)
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
                List<ReplayableMessage> currentReplayables = ctx.ReplayableMessages
                    .Where(GetMessageSelector<ReplayableMessage>(senderNickLower, senderUserLower))
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

        protected virtual void RegularPrivateMessageDelivery(IChannelMessageEventArgs args, string senderUserLower)
        {
            string senderNickLower = args.SenderNickname.ToLowerInvariant();

            using (var ctx = GetNewContext())
            {
                // check if the sender should get any PMs
                List<PrivateMessage> privateMessages = ctx.PrivateMessages
                    .Where(GetMessageSelector<PrivateMessage>(senderNickLower, senderUserLower))
                    .OrderBy(m => m.ID)
                    .ToList()
                ;

                if (privateMessages.Count == 0)
                {
                    // meh
                    return;
                }
                else if (privateMessages.Count == 1)
                {
                    // one message
                    Logger.LogDebug(
                        "delivering {Sender}'s private message {Message} to {Recipient}",
                        StringUtil.LiteralString(privateMessages[0].SenderOriginal),
                        StringUtil.LiteralString(privateMessages[0].Body),
                        StringUtil.LiteralString(args.SenderNickname)
                    );
                    ConnectionManager.SendQueryMessageFormat(
                        args.SenderNickname,
                        "PM! {0} <{1}> {2}",
                        FormatUtcTimestampFromDatabase(privateMessages[0].Timestamp),
                        privateMessages[0].SenderOriginal,
                        privateMessages[0].Body
                    );
                }
                else
                {
                    // multiple messages
                    ConnectionManager.SendQueryMessageFormat(
                        args.SenderNickname,
                        "{0} PMs!",
                        privateMessages.Count
                    );
                    foreach (var msg in privateMessages)
                    {
                        Logger.LogDebug(
                            "delivering {Sender}'s private message {Message} to {Recipient} as part of a chunk",
                            StringUtil.LiteralString(msg.SenderOriginal),
                            StringUtil.LiteralString(msg.Body),
                            StringUtil.LiteralString(args.SenderNickname)
                        );
                        ConnectionManager.SendQueryMessageFormat(
                            args.SenderNickname,
                            "{0} <{1}> {2}",
                            FormatUtcTimestampFromDatabase(msg.Timestamp),
                            msg.SenderOriginal,
                            msg.Body
                        );
                    }
                    ConnectionManager.SendQueryMessage(args.SenderNickname, "Cheers!");
                }

                // remove the messages from the delivery queue
                ctx.PrivateMessages.RemoveRange(privateMessages);

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
                .Where(m => m.RecipientLowercase == lowerOldNick && !m.ExactNickname);
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

        static Expression<Func<TMessage, bool>> GetMessageSelector<TMessage>(string recipientNickLower,
                string recipientUserLower)
            where TMessage : IMessage
        {
            return m =>
                (!m.ExactNickname && m.RecipientLowercase == recipientUserLower)
                || (m.ExactNickname && m.RecipientLowercase == recipientNickLower)
            ;
        }
    }
}
