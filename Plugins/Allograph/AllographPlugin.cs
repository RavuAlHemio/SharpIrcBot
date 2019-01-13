using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Chunks;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Libraries.RegularExpressionReplacement;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Allograph
{
    public class AllographPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<AllographPlugin>();

        protected AllographConfig Config { get; set; }
        protected Random Random { get; }
        protected IConnectionManager ConnectionManager { get; }
        protected List<ReplacerRegex> ReplacerRegexes { get; }
        protected Dictionary<string, List<int>> CooldownsPerChannel { get; }

        public AllographPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new AllographConfig(config);
            Random = new Random();
            CooldownsPerChannel = new Dictionary<string, List<int>>();
            ReplacerRegexes = new List<ReplacerRegex>();

            RecompileReplacerRegexes();

            ConnectionManager.ChannelMessage += HandleChannelMessage;

            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("allostats"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        new RegexMatcher("^[#&]\\S{1,256}$").ToRequiredWordTaker(), // channel name
                        RestTaker.Instance // test message (optional)
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleAlloStatsCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new AllographConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            CooldownsPerChannel.Clear();
            RecompileReplacerRegexes();
        }

        protected virtual void RecompileReplacerRegexes()
        {
            ReplacerRegexes.Clear();
            foreach (AllographConfig.Replacement replacement in Config.Replacements)
            {
                ReplacerRegexes.Add(new ReplacerRegex(replacement.Regex, replacement.ReplacementString));
            }
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

            var lookups = new Dictionary<string, string>
            {
                ["username"] = args.SenderNickname
            };

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
                bool fullReplacement = false;
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

                    // perform the replacement
                    string nextNewChunk = ReplacerRegexes[i].Replace(newChunk, lookups);

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

                            if (repl.ReplaceFullMessage)
                            {
                                // the replacement shall become the whole new body
                                newBody.Clear();
                                newBody.Append(newChunk);

                                // stop looping here
                                fullReplacement = true;
                                break;
                            }
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

                        if (repl.ReplaceFullMessage && !string.Equals(newChunk, nextNewChunk, StringComparison.Ordinal))
                        {
                            // the replacement shall become the whole new body
                            newBody.Clear();
                            newBody.Append(newChunk);

                            // stop looping here
                            fullReplacement = true;
                            break;
                        }
                    }
                }

                if (Config.CooldownIncreasePerHit >= 0)
                {
                    // update cooldowns
                    CooldownsPerChannel[channel].Clear();
                    CooldownsPerChannel[channel].AddRange(newCooldowns);

                    Logger.LogDebug("cooldowns are now: {Cooldowns}", newCooldowns.Select(c => c.ToString()).StringJoin(", "));
                }

                if (fullReplacement)
                {
                    // the body has been fully replaced
                    break;
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
                Logger.LogDebug("{RandomProbability} < {ConfigProbability}; posting {Body}", thisProbabilityValue, Config.ProbabilityPercent, newBody);
                ConnectionManager.SendChannelMessage(args.Channel, newBody.ToString());
            }
            else
            {
                Logger.LogDebug("{RandomProbability} >= {ConfigProbability}; not posting {Body}", thisProbabilityValue, Config.ProbabilityPercent, newBody);
            }
        }

        protected virtual void HandleAlloStatsCommand(CommandMatch cmd, IPrivateMessageEventArgs msg)
        {
            string nick = msg.SenderNickname;
            string registeredNick = ConnectionManager.RegisteredNameForNick(nick);
            if (registeredNick == null || !Config.Stewards.Contains(registeredNick))
            {
                // nope
                return;
            }

            string channel = ((Match)cmd.Arguments[0]).Value;
            if (!CooldownsPerChannel.ContainsKey(channel))
            {
                ConnectionManager.SendQueryMessageFormat(nick, "No cooldowns for {0}.", channel);
                return;
            }

            string testMessage = ((string)cmd.Arguments[1]).Trim();
            if (testMessage.Length == 0)
            {
                testMessage = null;
            }

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
