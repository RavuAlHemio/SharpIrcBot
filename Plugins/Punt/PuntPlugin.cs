using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Collections;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Punt
{
    public class PuntPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<PuntPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected PuntConfig Config { get; set; }
        protected Random Randomizer { get; }
        protected RegexCache RegexCache { get; }

        public PuntPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new PuntConfig(config);
            Randomizer = new Random();
            RegexCache = new RegexCache();

            ConnectionManager.ChannelAction += HandleAnyChannelMessage;
            ConnectionManager.ChannelMessage += HandleAnyChannelMessage;
            ConnectionManager.ChannelNotice += HandleAnyChannelMessage;

            RebuildRegexCache();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new PuntConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            RebuildRegexCache();
        }

        protected virtual void RebuildRegexCache()
        {
            var newPatterns = new HashSet<string>();

            IEnumerable<PuntPattern> allChannelPatterns = Config.ChannelsPatterns.Values
                .Where(channelPattern => channelPattern != null)
                .SelectMany(channelPattern => channelPattern);
            IEnumerable<PuntPattern> allPatterns = Config.CommonPatterns.Concat(allChannelPatterns);

            foreach (PuntPattern pattern in allPatterns)
            {
                IEnumerable<string> allRegexes = pattern.NickPatterns
                    .Concat(pattern.NickExceptPatterns)
                    .Concat(pattern.BodyPatterns)
                    .Concat(pattern.BodyExceptPatterns);

                foreach (string regex in allRegexes)
                {
                    newPatterns.Add(regex);
                }
            }

            RegexCache.ReplaceAllWith(newPatterns);
        }

        protected virtual void HandleAnyChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (!Config.ChannelsPatterns.ContainsKey(e.Channel))
            {
                // don't police this channel
                return;
            }

            IEnumerable<PuntPattern> relevantPatterns = Config.CommonPatterns;

            IEnumerable<PuntPattern> channelPatterns = Config.ChannelsPatterns[e.Channel];
            if (channelPatterns != null)
            {
                relevantPatterns = relevantPatterns.Concat(channelPatterns);
            }

            string normalizedNick = ConnectionManager.RegisteredNameForNick(e.SenderNickname) ?? e.SenderNickname;
            foreach (PuntPattern pattern in relevantPatterns)
            {
                if (!AnyMatch(normalizedNick, pattern.NickPatterns))
                {
                    // wrong user
                    continue;
                }

                if (AnyMatch(normalizedNick, pattern.NickExceptPatterns))
                {
                    // whitelisted user
                    continue;
                }

                if (pattern.ChancePercent.HasValue)
                {
                    int val = Randomizer.Next(100);
                    if (val >= pattern.ChancePercent.Value)
                    {
                        // luck is on their side
                        continue;
                    }
                }

                if (!AnyMatch(e.Message, pattern.BodyPatterns))
                {
                    // no body match
                    continue;
                }

                if (AnyMatch(e.Message, pattern.BodyExceptPatterns))
                {
                    // body exception
                    continue;
                }

                // match! kick 'em!
                ConnectionManager.KickChannelUser(e.Channel, e.SenderNickname, pattern.KickMessage);
                return;
            }
        }

        protected bool AnyMatch(string text, List<string> regexes)
        {
            foreach (string regex in regexes)
            {
                Regex regexObject = RegexCache[regex];
                if (regexObject.IsMatch(text))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
