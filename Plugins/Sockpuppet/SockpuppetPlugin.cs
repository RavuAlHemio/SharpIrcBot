using System;
using System.Reflection;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using Meebey.SmartIrc4net;

namespace Sockpuppet
{
    public class SockpuppetPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ConnectionManager ConnectionManager { get; }
        protected SockpuppetConfig Config { get; set; }

        public SockpuppetPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SockpuppetConfig(config);

            ConnectionManager.QueryMessage += HandleQueryMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new SockpuppetConfig(newConfig);
        }

        private void HandleQueryMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleQueryMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected string VerifyIdentity(IrcMessageData message)
        {
            var username = message.Nick;
            if (Config.UseIrcServices)
            {
                username = ConnectionManager.RegisteredNameForNick(username);
                if (username == null)
                {
                    Logger.InfoFormat("{0} is not logged in; ignoring", message.Nick);
                    return null;
                }
            }

            if (!Config.Puppeteers.Contains(username))
            {
                Logger.InfoFormat("{0} is not a puppeteer; ignoring", username);
                return null;
            }

            return username;
        }

        protected void PerformSockpuppet(string username, string nick, string message)
        {
            var command = message.Substring("!sockpuppet ".Length);

            var unescapedCommand = SharpIrcBotUtil.UnescapeString(command);
            if (unescapedCommand == null)
            {
                Logger.InfoFormat("{0} bollocksed up their escapes; ignoring", username);
                return;
            }

            Logger.InfoFormat("{0} (nick: {1}) issued the following command: {2}", username, nick, command);

            ConnectionManager.SendRawCommand(unescapedCommand);
        }

        protected void ActuallyHandleQueryMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var message = args.Data;
            if (message.Type != ReceiveType.QueryMessage || message.Nick == ConnectionManager.Client.Nickname)
            {
                return;
            }

            if (message.Message.StartsWith("!sockpuppet "))
            {
                string username = VerifyIdentity(message);
                if (username == null)
                {
                    return;
                }
                PerformSockpuppet(username, message.Nick, message.Message);
                return;
            }

            if (message.Message == "!reload")
            {
                string username = VerifyIdentity(message);
                if (username == null)
                {
                    return;
                }
                ConnectionManager.ReloadConfiguration();
                return;
            }
        }
    }
}
