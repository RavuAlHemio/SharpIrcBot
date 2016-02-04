using System;
using System.Linq;
using System.Reflection;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace TextCommands
{
    public class TextCommandsPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly TextCommandsConfig Config;
        protected readonly ConnectionManager ConnectionManager;

        public TextCommandsPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new TextCommandsConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandlePrivateMessage;
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleMessage(
                    message => ConnectionManager.SendChannelMessage(args.Data.Channel, message),
                    args,
                    flags
                );
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        private void HandlePrivateMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleMessage(
                    message => ConnectionManager.SendQueryMessage(args.Data.Nick, message),
                    args,
                    flags
                );
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected void ActuallyHandleMessage(Action<string> respond, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var message = args.Data;
            if (message.Nick == ConnectionManager.Client.Nickname)
            {
                return;
            }

            var lowerBody = message.Message.ToLowerInvariant();

            if (Config.CommandsResponses.ContainsKey(lowerBody))
            {
                Logger.DebugFormat("{0} triggered {1} in {2}", message.Nick, lowerBody, message.Channel);
                var response = Config.CommandsResponses[lowerBody].Replace("{{NICKNAME}}", message.Nick);
                foreach (var line in response.Split('\n').Where(l => l.Length > 0))
                {
                    respond(line);
                }
            }
        }
    }
}
