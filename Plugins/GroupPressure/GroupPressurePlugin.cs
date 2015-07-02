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
    public class GroupPressurePlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected Queue<BacklogMessage> Backlog;
        protected PressureConfig Config;
        protected ConnectionManager Connection;

        public GroupPressurePlugin(ConnectionManager connMgr, JObject config)
        {
            Backlog = new Queue<BacklogMessage>();
            Config = new PressureConfig(config);
            Connection = connMgr;

            Connection.ChannelMessage += HandleChannelMessageOrAction;
            Connection.ChannelAction += HandleChannelMessageOrAction;
        }

        private void HandleChannelMessageOrAction(object sender, IrcEventArgs e)
        {
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

            // append the message
            Backlog.Enqueue(new BacklogMessage
            {
                Sender = e.Data.Nick,
                Body = body,
                Action = (e.Data.Type == ReceiveType.ChannelAction)
            });

            // perform accounting
            var messageToSenders = new Dictionary<string, HashSet<string>>();
            foreach (var backMessage in Backlog)
            {
                var actualBody = (backMessage.Action ? 'A' : 'M') + backMessage.Body;
                if (backMessage.Sender == Connection.Config.Nickname)
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
                Connection.Client.SendMessage(msg[0] == 'A' ? SendType.Action : SendType.Message, e.Data.Channel, msg.Substring(1));

                // fake this message into the backlog to prevent duplicates
                Backlog.Enqueue(new BacklogMessage
                {
                    Sender = Connection.Config.Username,
                    Body = body,
                    Action = (e.Data.Type == ReceiveType.ChannelAction)
                });
            }
        }
    }
}
