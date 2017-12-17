using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Collections;
using SharpIrcBot.Commands;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Counters.ORM;

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

            // go back through time
            RingBuffer<ChannelMessage> messages;
            if (!ChannelsMessages.TryGetValue(msg.Channel, out messages))
            {
                return;
            }

            IEnumerable<ChannelMessage> messagesReversed = messages
                .Reverse();
            ChannelMessage foundMessage = null;
            foreach (ChannelMessage message in messagesReversed)
            {
                if (!message.Body.Contains(findMe))
                {
                    continue;
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
                    msg.Channel,
                    $"{msg.SenderNickname}: Nothing to count."
                );
                return;
            }

            if (foundMessage.Counted)
            {
                ConnectionManager.SendChannelMessage(
                    msg.Channel,
                    $"{msg.SenderNickname}: That's been counted already."
                );
                return;
            }

            using (CountersContext ctx = GetNewContext())
            {
                ctx.Entries.Add(new CounterEntry
                {
                    Command = commandMatch.CommandName,
                    HappenedTimestamp = foundMessage.Timestamp,
                    CountedTimestamp = DateTimeOffset.Now,
                    Channel = msg.Channel,
                    PerpNickname = foundMessage.Nickname,
                    PerpUsername = foundMessage.Username,
                    CounterNickname = msg.SenderNickname,
                    CounterUsername = ConnectionManager.RegisteredNameForNick(msg.SenderNickname),
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
                    ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: Already expunged: <{entry.PerpNickname}> {args.Message}");
                    return;
                }

                entry.Expunged = true;
                ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: Okay, expunged <{entry.PerpNickname}> {args.Message}");

                ctx.SaveChanges();
            }
        }

        private CountersContext GetNewContext()
        {
            DbContextOptions<CountersContext> opts = SharpIrcBotUtil.GetContextOptions<CountersContext>(Config);
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
