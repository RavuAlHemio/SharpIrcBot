using System;
using System.Reflection;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using Meebey.SmartIrc4net;

namespace Sockpuppet
{
    public class SockpuppetPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ConnectionManager ConnectionManager;
        protected SockpuppetConfig Config;

        public SockpuppetPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SockpuppetConfig(config);

            ConnectionManager.QueryMessage += HandleQueryMessage;
        }

        private void HandleQueryMessage(object sender, IrcEventArgs args)
        {
            try
            {
                ActuallyHandleQueryMessage(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected void ActuallyHandleQueryMessage(object sender, IrcEventArgs args)
        {
            var message = args.Data;
            if (message.Type != ReceiveType.QueryMessage || message.Nick == ConnectionManager.Client.Nickname)
            {
                return;
            }

            if (!message.Message.StartsWith("!sockpuppet "))
            {
                return;
            }

            var command = message.Message.Substring("!sockpuppet ".Length);

            var username = message.Nick;
            if (Config.UseIrcServices)
            {
                username = ConnectionManager.RegisteredNameForNick(username);
                if (username == null)
                {
                    Logger.InfoFormat("{0} is not logged in; ignoring", message.Nick);
                    return;
                }
            }

            if (!Config.Puppeteers.Contains(username))
            {
                Logger.InfoFormat("{0} is not a puppeteer; ignoring", username);
                return;
            }

            Logger.InfoFormat("{0} (nick: {1}) issued the following command: {2}", username, message.Nick, command);

            ConnectionManager.SendRawCommand(command);
        }
    }
}
