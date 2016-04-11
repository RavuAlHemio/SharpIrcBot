using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace GroupPressure
{
    /// <summary>
    /// Submit to group pressure: if enough people say a specific thing in the last X messages,
    /// join in on the fray!
    /// </summary>
    public class GroupPressurePlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected Queue<BacklogMessage> Backlog;
        protected PressureConfig Config;
        protected IConnectionManager Connection;

        public GroupPressurePlugin(IConnectionManager connMgr, JObject config)
        {
            Backlog = new Queue<BacklogMessage>();
            Config = new PressureConfig(config);
            Connection = connMgr;

            Connection.ChannelMessage += HandleChannelMessageOrAction;
            Connection.ChannelAction += HandleChannelMessageOrAction;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new PressureConfig(newConfig);
        }

        private void HandleChannelMessageOrAction(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessageOrAction(sender, args, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        private void ActuallyHandleChannelMessageOrAction(object sender, IrcEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var body = e.Data.Message;
            if (body.Length == 0)
            {
                return;
            }

            if (!Config.Channels.Contains(e.Data.Channel))
            {
                return;
            }

            // clean out the backlog
            while (Backlog.Count > Config.BacklogSize)
            {
                Backlog.Dequeue();
            }

            var normalizedSender = Connection.RegisteredNameForNick(e.Data.Nick) ?? e.Data.Nick;

            // append the message
            Backlog.Enqueue(new BacklogMessage
            {
                Sender = normalizedSender,
                Body = body,
                Action = (e.Data.Type == ReceiveType.ChannelAction)
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

                Logger.DebugFormat(
                    "bowing to the group pressure of ({0}) sending {1}",
                    string.Join(", ", senders.Select(s => SharpIrcBotUtil.LiteralString(s))),
                    SharpIrcBotUtil.LiteralString(msg)
                );

                // submit to group pressure
                if (msg[0] == 'A')
                {
                    Connection.SendChannelAction(e.Data.Channel, msg.Substring(1));
                }
                else
                {
                    Connection.SendChannelMessage(e.Data.Channel, msg.Substring(1));
                }

                // fake this message into the backlog to prevent duplicates
                Backlog.Enqueue(new BacklogMessage
                {
                    Sender = Connection.MyUsername,
                    Body = body,
                    Action = (e.Data.Type == ReceiveType.ChannelAction)
                });
            }
        }
    }
}
