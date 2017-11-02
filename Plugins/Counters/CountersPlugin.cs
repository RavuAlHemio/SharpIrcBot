using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Collections;
using SharpIrcBot.Commands;
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

            ConnectionManager.ChannelMessage += HandleChannelMessage;
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
                    CommandUtil.NoArguments,
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
                    Timestamp = DateTimeOffset.Now,
                    Channel = msg.Channel,
                    PerpNickname = foundMessage.Nickname,
                    PerpUsername = foundMessage.Username,
                    CounterNickname = msg.SenderNickname,
                    CounterUsername = ConnectionManager.RegisteredNameForNick(msg.SenderNickname),
                    Message = foundMessage.Body
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

            if (args.Message.StartsWith(ConnectionManager.CommandManager.Config.CommandPrefix))
            {
                if (args.Message.TrimEnd().IndexOf(' ') == -1)
                {
                    // starts with a command character and has no spaces
                    // do not consider this message relevant
                    return;
                }
            }

            RingBuffer<ChannelMessage> messages = GetOrCreateValue(
                ChannelsMessages, args.Channel, chan => new RingBuffer<ChannelMessage>(Config.BacklogSize)
            );

            messages.Add(new ChannelMessage
            {
                Nickname = args.SenderNickname,
                Username = ConnectionManager.RegisteredNameForNick(args.SenderNickname),
                Body = args.Message,
                Counted = false
            });
        }

        private CountersContext GetNewContext()
        {
            var opts = SharpIrcBotUtil.GetContextOptions<CountersContext>(Config);
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
