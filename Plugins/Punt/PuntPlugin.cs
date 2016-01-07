using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Punt
{
    public class PuntPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ConnectionManager ConnectionManager { get; }
        protected PuntConfig Config { get; }
        protected Dictionary<string, Regex> RegexCache { get; }

        public PuntPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new PuntConfig(config);
            RegexCache = new Dictionary<string, Regex>(Config.Patterns.Count);

            ConnectionManager.ChannelAction += HandleChannelAction;
            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.ChannelNotice += HandleChannelNotice;
        }

        protected void HandleChannelAction(object sender, ActionEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleMessage(e.Data.Channel, e.Data.Nick, e.ActionMessage);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling channel action", exc);
            }
        }

        protected void HandleChannelMessage(object sender, IrcEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleMessage(e.Data.Channel, e.Data.Nick, e.Data.Message);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling channel message", exc);
            }
        }

        protected void HandleChannelNotice(object sender, IrcEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleMessage(e.Data.Channel, e.Data.Nick, e.Data.Message);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling channel notice", exc);
            }
        }

        protected virtual void ActuallyHandleMessage(string channel, string nick, string body)
        {
            foreach (var kvp in Config.Patterns)
            {
                if (!RegexCache.ContainsKey(kvp.Key))
                {
                    RegexCache[kvp.Key] = new Regex(kvp.Key);
                }

                if (RegexCache[kvp.Key].IsMatch(body))
                {
                    // match! kick 'em!
                    ConnectionManager.Client.RfcKick(channel, nick, kvp.Value);
                    return;
                }
            }
        }
    }
}
