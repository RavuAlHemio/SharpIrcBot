using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Collections;
using SharpIrcBot.Commands;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Counters.ORM;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Counters
{
    public class CountersPlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected CountersConfig Config { get; set; }
        protected HashSet<Command> CurrentCommands { get; set; }
        protected Dictionary<string, RingBuffer<ChannelMessage>> ChannelsMessages { get; set; }

        public CountersPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new CountersConfig(config);
            CurrentCommands = new HashSet<Command>();
            ChannelsMessages = new Dictionary<string, RingBuffer<ChannelMessage>>();

            ReregisterCommands();

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("uncount"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // counter name
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleUncountCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("regexcount"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // counter name
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // nickname
                        RestTaker.Instance // regex
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleRegexCountCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("counted"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // counter name
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleCountedCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("counterstats"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // counter name
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleCounterStatsCommand
            );

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.BaseNickChanged += HandleBaseNickChanged;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new CountersConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            ReregisterCommands();
        }

        protected virtual void ReregisterCommands()
        {
            var currentCommandCopy = new List<Command>(CurrentCommands);
            foreach (Command command in currentCommandCopy)
            {
                ConnectionManager.CommandManager.UnregisterChannelMessageCommandHandler(
                    command,
                    HandleCounterCommand
                );
                CurrentCommands.Remove(command);
            }

            foreach (Counter counter in Config.Counters)
            {
                var command = new Command(
                    CommandUtil.MakeNames(counter.CommandName),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(RestTaker.Instance),
                    forbiddenFlags: MessageFlags.UserBanned
                );
                ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                    command,
                    HandleCounterCommand
                );
                CurrentCommands.Add(command);
            }
        }

        protected virtual void HandleCounterCommand(CommandMatch commandMatch, IChannelMessageEventArgs msg)
        {
            Counter counter = Config.Counters.FirstOrDefault(c => c.CommandName == commandMatch.CommandName);
            if (counter == null)
            {
                // counter gone
                return;
            }

            string findMe = ((string)commandMatch.Arguments[0]).Trim();
            TryToMatch(counter, msg.Channel, msg.SenderNickname, messageSubstring: findMe, messageRegex: null);
        }

        protected virtual void TryToMatch(Counter counter, string channel, string senderNickname,
                string messageSubstring = null, Regex messageRegex = null)
        {
            // go back through time
            RingBuffer<ChannelMessage> messages;
            if (!ChannelsMessages.TryGetValue(channel, out messages))
            {
                return;
            }

            IEnumerable<ChannelMessage> messagesReversed = messages
                .Reverse();
            ChannelMessage foundMessage = null;
            foreach (ChannelMessage message in messagesReversed)
            {
                if (messageSubstring != null)
                {
                    if (!message.Body.Contains(messageSubstring))
                    {
                        continue;
                    }
                }
                if (messageRegex != null)
                {
                    if (!messageRegex.IsMatch(message.Body))
                    {
                        continue;
                    }
                }

                if (counter.MessageRegex != null && !counter.MessageRegex.IsMatch(message.Body))
                {
                    continue;
                }

                if (
                    counter.NicknameRegex != null
                    && !counter.NicknameRegex.IsMatch(message.Nickname)
                    && (message.Username == null || !counter.NicknameRegex.IsMatch(message.Username))
                )
                {
                    continue;
                }

                foundMessage = message;
                break;
            }

            if (foundMessage == null)
            {
                ConnectionManager.SendChannelMessage(
                    channel,
                    $"{senderNickname}: Nothing to count."
                );
                return;
            }

            if (foundMessage.Counted)
            {
                ConnectionManager.SendChannelMessage(
                    channel,
                    $"{senderNickname}: That's been counted already."
                );
                return;
            }

            using (CountersContext ctx = GetNewContext())
            {
                ctx.Entries.Add(new CounterEntry
                {
                    Command = counter.CommandName,
                    HappenedTimestamp = foundMessage.Timestamp,
                    CountedTimestamp = DateTimeOffset.Now,
                    Channel = channel,
                    PerpNickname = foundMessage.Nickname,
                    PerpUsername = foundMessage.Username,
                    CounterNickname = senderNickname,
                    CounterUsername = ConnectionManager.RegisteredNameForNick(senderNickname),
                    Message = foundMessage.Body,
                    Expunged = false
                });
                ctx.SaveChanges();
            }

            foundMessage.Counted = true;
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (args.SenderNickname == ConnectionManager.MyNickname)
            {
                return;
            }

            RingBuffer<ChannelMessage> messages = GetOrCreateValue(
                ChannelsMessages, args.Channel, chan => new RingBuffer<ChannelMessage>(Config.BacklogSize)
            );

            messages.Add(new ChannelMessage
            {
                Nickname = args.SenderNickname,
                Username = ConnectionManager.RegisteredNameForNick(args.SenderNickname),
                Body = args.Message,
                Timestamp = DateTimeOffset.Now,
                Counted = false
            });
        }

        protected virtual void HandleBaseNickChanged(object sender, BaseNickChangedEventArgs e)
        {
            string oldBaseNick = e.OldBaseNick;
            string newBaseNick = e.NewBaseNick;

            using (var ctx = GetNewContext())
            {
                IQueryable<CounterEntry> matchedEntries;
                
                matchedEntries = ctx.Entries
                    .Where(u => u.CounterUsername == oldBaseNick);
                foreach (CounterEntry entry in matchedEntries)
                {
                    entry.CounterUsername = newBaseNick;
                }
                ctx.SaveChanges();

                matchedEntries = ctx.Entries
                    .Where(u => u.PerpUsername == oldBaseNick);
                foreach (CounterEntry entry in matchedEntries)
                {
                    entry.PerpUsername = newBaseNick;
                }
                ctx.SaveChanges();
            }
        }

        protected virtual void HandleCountedCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            var counter = (string)cmd.Arguments[0];
            CounterEntry entry;
            using (CountersContext ctx = GetNewContext())
            {
                entry = ctx.Entries
                    .Where(e => e.Command == counter && e.Channel == args.Channel && !e.Expunged)
                    .OrderByDescending(e => e.ID)
                    .FirstOrDefault()
                ;

                if (entry == null)
                {
                    ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: Found nothing for counter '{counter}' in {args.Channel}.");
                    return;
                }

                ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: <{entry.PerpNickname}> {entry.Message}");
            }
        }

        protected virtual void HandleUncountCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            ChannelUserLevel level = ConnectionManager.GetChannelLevelForUser(args.Channel, args.SenderNickname);
            if (level < ChannelUserLevel.HalfOp)
            {
                ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: You need to be a channel operator.");
                return;
            }

            var counter = (string)cmd.Arguments[0];
            CounterEntry entry;
            using (CountersContext ctx = GetNewContext())
            {
                entry = ctx.Entries
                    .Where(e => e.Command == counter && e.Channel == args.Channel)
                    .OrderByDescending(e => e.ID)
                    .FirstOrDefault()
                ;

                if (entry == null)
                {
                    ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: Found nothing for counter '{counter}' in {args.Channel}.");
                    return;
                }

                if (entry.Expunged)
                {
                    ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: Already expunged: <{entry.PerpNickname}> {entry.Message}");
                    return;
                }

                entry.Expunged = true;
                ctx.SaveChanges();

                ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: Okay, expunged <{entry.PerpNickname}> {entry.Message}");
            }
        }

        protected virtual void HandleRegexCountCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            var counterName = (string)cmd.Arguments[0];
            var nick = (string)cmd.Arguments[1];
            string regexString = ((string)cmd.Arguments[2]).Trim();

            Counter counter = Config.Counters.FirstOrDefault(c => c.CommandName == counterName);
            if (counter == null)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: Unknown counter '{counterName}'");
                return;
            }

            Regex regex;
            try
            {
                regex = new Regex(regexString, RegexOptions.Compiled);
            }
            catch (ArgumentException ae)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: Invalid regex: {ae.Message}");
                return;
            }

            TryToMatch(counter, msg.Channel, msg.SenderNickname, messageSubstring: null, messageRegex: regex);
        }

        protected virtual void HandleCounterStatsCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            var counter = (string)cmd.Arguments[0];
            using (CountersContext ctx = GetNewContext())
            {
                IQueryable<CounterEntry> relevantEntries = ctx.Entries
                    .Where(e => e.Command == counter && e.Channel == args.Channel && !e.Expunged);

                int entryCount = relevantEntries.Count();
                List<KeyValuePair<string, int>> usersCounts = relevantEntries
                    .GroupBy(e => e.PerpUsername ?? e.PerpNickname, (key, es) => new KeyValuePair<string, int>(key, es.Count()))
                    .OrderByDescending(uc => uc.Value)
                    .Take(Config.TopCount + 1)
                    .ToList();

                string tops = usersCounts
                    .Select(uc => $"{uc.Key}: {uc.Value}")
                    .Take(Config.TopCount)
                    .StringJoin(", ");

                string topText;
                if (usersCounts.Count == 0)
                {
                    topText = "";
                }
                else if (usersCounts.Count <= Config.TopCount)
                {
                    topText = $" (top: {tops})";
                }
                else
                {
                    topText = $" (top {Config.TopCountText}: {tops})";
                }

                ConnectionManager.SendChannelMessage(
                    args.Channel,
                    $"{args.SenderNickname}: '{counter}': {entryCount}{topText}"
                );
            }
        }

        private CountersContext GetNewContext()
        {
            DbContextOptions<CountersContext> opts = DatabaseUtil.GetContextOptions<CountersContext>(Config);
            return new CountersContext(opts);
        }

        private static TValue GetOrCreateValue<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key,
                Func<TKey, TValue> generator)
        {
            TValue ret;
            if (!dict.TryGetValue(key, out ret))
            {
                ret = generator.Invoke(key);
                dict[key] = ret;
            }
            return ret;
        }
    }
}
