using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.GroupPressure
{
    /// <summary>
    /// Submit to group pressure: if enough people say a specific thing in the last X messages,
    /// join in on the fray!
    /// </summary>
    public class GroupPressurePlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<GroupPressurePlugin>();

        protected Queue<BacklogMessage> Backlog;
        protected PressureConfig Config;
        protected IConnectionManager Connection;

        public GroupPressurePlugin(IConnectionManager connMgr, JObject config)
        {
            Backlog = new Queue<BacklogMessage>();
            Config = new PressureConfig(config);
            Connection = connMgr;

            Connection.ChannelMessage += HandleChannelMessage;
            Connection.ChannelAction += HandleChannelAction;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new PressureConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        private void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            ActuallyHandleChannelMessageOrAction(sender, args, flags, action: false);
        }

        private void HandleChannelAction(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            ActuallyHandleChannelMessageOrAction(sender, args, flags, action: true);
        }

        private void ActuallyHandleChannelMessageOrAction(object sender, IChannelMessageEventArgs e, MessageFlags flags, bool action)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var body = e.Message;
            if (body.Length == 0)
            {
                return;
            }

            if (!Config.Channels.Contains(e.Channel))
            {
                return;
            }

            // clean out the backlog
            while (Backlog.Count > Config.BacklogSize)
            {
                Backlog.Dequeue();
            }

            var normalizedSender = Connection.RegisteredNameForNick(e.SenderNickname) ?? e.SenderNickname;

            // append the message
            Backlog.Enqueue(new BacklogMessage
            {
                Sender = normalizedSender,
                Body = body,
                Action = action
            });

            // perform accounting
            var messageToSenders = new Dictionary<string, HashSet<string>>();
            foreach (var backMessage in Backlog)
            {
                var actualBody = (backMessage.Action ? 'A' : 'M') + backMessage.Body;
                if (backMessage.Sender == Connection.MyNickname)
                {
                    // this is my message -- start counting from zero, so to speak
                    messageToSenders[actualBody] = new HashSet<string>();
                }
                else
                {
                    if (!messageToSenders.ContainsKey(actualBody))
                    {
                        messageToSenders[actualBody] = new HashSet<string>();
                    }
                    messageToSenders[actualBody].Add(backMessage.Sender);
                }
            }

            foreach (var messageAndSenders in messageToSenders)
            {
                var msg = messageAndSenders.Key;
                var senders = messageAndSenders.Value;
                if (senders.Count < Config.TriggerCount)
                {
                    continue;
                }

                Logger.LogDebug(
                    "bowing to the group pressure of ({Senders}) sending {Message}",
                    senders.Select(s => StringUtil.LiteralString(s)).StringJoin(", "),
                    StringUtil.LiteralString(msg)
                );

                // submit to group pressure
                bool lastAction;
                if (msg[0] == 'A')
                {
                    Connection.SendChannelAction(e.Channel, msg.Substring(1));
                    lastAction = true;
                }
                else
                {
                    Connection.SendChannelMessage(e.Channel, msg.Substring(1));
                    lastAction = false;
                }

                // fake this message into the backlog to prevent duplicates
                Backlog.Enqueue(new BacklogMessage
                {
                    Sender = Connection.MyUsername,
                    Body = body,
                    Action = lastAction
                });
            }
        }
    }
}
