using System;
using System.Reflection;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Allograph
{
    public class AllographPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly AllographConfig Config;
        protected readonly Random Random;
        protected readonly ConnectionManager ConnectionManager;
        
        public AllographPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new AllographConfig(config);
            Random = new Random();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected void ActuallyHandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var message = args.Data;
            if (message.Type != ReceiveType.ChannelMessage || message.Nick == ConnectionManager.Client.Nickname)
            {
                return;
            }

            var originalBody = message.Message;
            var newBody = originalBody;

            foreach (var repl in Config.Replacements)
            {
                if (repl.OnlyIfPrecedingHit && string.Equals(newBody, originalBody, StringComparison.InvariantCulture))
                {
                    // no preceding rule hit; don't apply this one
                    continue;
                }

                // substitute the username in the replacement string
                var replacementStringWithUser = repl.ReplacementString.Replace("{{{username}}}", message.Nick);
                newBody = repl.Regex.Replace(newBody, replacementStringWithUser);
            }

            if (string.Equals(newBody, originalBody, StringComparison.InvariantCulture))
            {
                return;
            }

            var thisProbabilityValue = Random.NextDouble() * 100.0;
            if (thisProbabilityValue < Config.ProbabilityPercent)
            {
                Logger.DebugFormat("{0:F2} < {1:F2}; posting {2}", thisProbabilityValue, Config.ProbabilityPercent, newBody);
                ConnectionManager.SendChannelMessage(message.Channel, newBody);
            }
            else
            {
                Logger.DebugFormat("{0:F2} >= {1:F2}; not posting {2}", thisProbabilityValue, Config.ProbabilityPercent, newBody);
            }
        }
    }
}
