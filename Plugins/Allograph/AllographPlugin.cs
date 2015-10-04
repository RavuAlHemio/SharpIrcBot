using System;
using System.Linq;
using System.Reflection;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using System.Collections.Generic;

namespace Allograph
{
    public class AllographPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly AllographConfig Config;
        protected readonly Random Random;
        protected readonly ConnectionManager ConnectionManager;
        protected readonly Dictionary<string, List<int>> CooldownsPerChannel;
        
        public AllographPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new AllographConfig(config);
            Random = new Random();
            CooldownsPerChannel = new Dictionary<string, List<int>>();

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
            var channel = message.Channel;

            if (Config.ChannelBlacklist.Contains(channel))
            {
                return;
            }

            if (!CooldownsPerChannel.ContainsKey(channel))
            {
                CooldownsPerChannel[channel] = new List<int>(Enumerable.Repeat(0, Config.Replacements.Count));
            }

            bool somethingHit = false;
            int i = -1;
            var newCooldowns = new List<int>(CooldownsPerChannel[channel]);
            foreach (var repl in Config.Replacements)
            {
                ++i;

                if (repl.OnlyIfPrecedingHit && !somethingHit)
                {
                    // no preceding rule hit; don't apply this one
                    continue;
                }

                if (repl.AdditionalProbabilityPercent > 0.0)
                {
                    var replProbabilityValue = Random.NextDouble() * 100.0;
                    if (replProbabilityValue >= repl.AdditionalProbabilityPercent)
                    {
                        // next!
                        continue;
                    }
                }

                // substitute the username in the replacement string
                var replacementStringWithUser = repl.ReplacementString.Replace("{{{username}}}", message.Nick);
                var nextNewBody = repl.Regex.Replace(newBody, replacementStringWithUser);

                if (Config.CooldownIncreasePerHit >= 0 || repl.CustomCooldownIncreasePerHit >= 0)
                {
                    if (!string.Equals(newBody, nextNewBody, StringComparison.InvariantCulture))
                    {
                        // this rule changed something!
                        if (newCooldowns[i] == 0)
                        {
                            // warm, apply it!
                            newBody = nextNewBody;
                            somethingHit = true;
                        }

                        // cool down
                        newCooldowns[i] += (repl.CustomCooldownIncreasePerHit >= 0)
                            ? repl.CustomCooldownIncreasePerHit
                            : Config.CooldownIncreasePerHit;
                    }
                    else if (newCooldowns[i] > 0)
                    {
                        // this rule didn't change anything; warm up!
                        --newCooldowns[i];
                    }
                }
                else
                {
                    // no cooldowns
                    newBody = nextNewBody;
                }
            }

            if (Config.CooldownIncreasePerHit >= 0)
            {
                // update cooldowns
                CooldownsPerChannel[channel].Clear();
                CooldownsPerChannel[channel].AddRange(newCooldowns);

                Logger.DebugFormat("cooldowns are now: {0}", string.Join(", ", newCooldowns.Select(c => c.ToString())));
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
