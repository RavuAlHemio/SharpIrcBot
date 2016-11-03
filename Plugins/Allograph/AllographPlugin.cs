using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using SharpIrcBot.Chunks;
using SharpIrcBot.Events.Irc;

namespace Allograph
{
    public class AllographPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<AllographPlugin>();
        public static readonly Regex StatsRegex = new Regex("^!allostats\\s+(?<channel>[#&]\\S+)(?:\\s+(?<testmsg>\\S+(?:\\s+\\S+)*))?\\s*$", RegexOptions.Compiled);

        protected AllographConfig Config;
        protected readonly Random Random;
        protected readonly IConnectionManager ConnectionManager;
        protected readonly Dictionary<string, List<int>> CooldownsPerChannel;
        
        public AllographPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new AllographConfig(config);
            Random = new Random();
            CooldownsPerChannel = new Dictionary<string, List<int>>();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandleQueryMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new AllographConfig(newConfig);
            CooldownsPerChannel.Clear();
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (args.SenderNickname == ConnectionManager.MyNickname)
            {
                return;
            }

            var originalBody = args.Message;
            var channel = args.Channel;

            if (Config.ChannelBlacklist.Contains(channel))
            {
                return;
            }

            if (!CooldownsPerChannel.ContainsKey(channel))
            {
                CooldownsPerChannel[channel] = new List<int>(Enumerable.Repeat(0, Config.Replacements.Count));
            }

            var chunks = ConnectionManager.SplitMessageToChunks(args.Message);
            var newBody = new StringBuilder();
            var newCooldowns = new List<int>(CooldownsPerChannel[channel]);
            foreach (var chunk in chunks)
            {
                var textChunk = chunk as TextMessageChunk;
                if (textChunk == null)
                {
                    // don't touch this
                    newBody.Append(chunk);
                    continue;
                }

                bool somethingHit = false;
                int i = -1;

                var newChunk = textChunk.Text;
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
                        var replProbabilityValue = Random.NextDouble()*100.0;
                        if (replProbabilityValue >= repl.AdditionalProbabilityPercent)
                        {
                            // next!
                            continue;
                        }
                    }

                    // substitute the username in the replacement string
                    var replacementStringWithUser = repl.ReplacementString.Replace("{{{username}}}", args.SenderNickname);
                    var nextNewChunk = repl.Regex.Replace(newChunk, replacementStringWithUser);

                    if (Config.CooldownIncreasePerHit >= 0 || repl.CustomCooldownIncreasePerHit >= 0)
                    {
                        if (!string.Equals(newChunk, nextNewChunk, StringComparison.Ordinal))
                        {
                            // this rule changed something!
                            if (newCooldowns[i] == 0)
                            {
                                // warm, apply it!
                                newChunk = nextNewChunk;
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
                        newChunk = nextNewChunk;
                    }
                }

                if (Config.CooldownIncreasePerHit >= 0)
                {
                    // update cooldowns
                    CooldownsPerChannel[channel].Clear();
                    CooldownsPerChannel[channel].AddRange(newCooldowns);

                    Logger.LogDebug("cooldowns are now: {0}", string.Join(", ", newCooldowns.Select(c => c.ToString())));
                }

                newBody.Append(newChunk);
            }

            if (string.Equals(newBody.ToString(), originalBody, StringComparison.Ordinal))
            {
                return;
            }

            var thisProbabilityValue = Random.NextDouble() * 100.0;
            if (thisProbabilityValue < Config.ProbabilityPercent)
            {
                Logger.LogDebug("{0:F2} < {1:F2}; posting {2}", thisProbabilityValue, Config.ProbabilityPercent, newBody);
                ConnectionManager.SendChannelMessage(args.Channel, newBody.ToString());
            }
            else
            {
                Logger.LogDebug("{0:F2} >= {1:F2}; not posting {2}", thisProbabilityValue, Config.ProbabilityPercent, newBody);
            }
        }

        protected virtual void HandleQueryMessage(object sender, IPrivateMessageEventArgs args, MessageFlags flags)
        {
            var nick = args.SenderNickname;
            var registeredNick = ConnectionManager.RegisteredNameForNick(nick);
            if (registeredNick == null || !Config.Stewards.Contains(registeredNick))
            {
                // nope
                return;
            }

            var match = StatsRegex.Match(args.Message);
            if (!match.Success)
            {
                return;
            }

            var channel = match.Groups["channel"].Value;
            if (!CooldownsPerChannel.ContainsKey(channel))
            {
                ConnectionManager.SendQueryMessageFormat(nick, "No cooldowns for {0}.", channel);
                return;
            }

            string testMessage = match.Groups["testmsg"].Success
                ? match.Groups["testmsg"].Value
                : null;

            ConnectionManager.SendQueryMessageFormat(nick, "Allograph stats for {0}:", channel);
            var cooldowns = CooldownsPerChannel[channel];
            for (int i = 0; i < Config.Replacements.Count; ++i)
            {
                var replacement = Config.Replacements[i];
                var cooldown = cooldowns[i];

                if (testMessage != null && !replacement.Regex.IsMatch(testMessage))
                {
                    // skip
                    continue;
                }

                ConnectionManager.SendQueryMessageFormat(nick, "{0} :::: {1}", replacement.RegexString, cooldown);
            }
            ConnectionManager.SendQueryMessage(nick, "End.");
        }
    }
}
