using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.DontJustHighlightMe
{
    public class DontJustHighlightMePlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<DontJustHighlightMePlugin>();

        protected IConnectionManager ConnectionManager { get; set; }
        protected DJHMConfig Config { get; set; }
        protected Random RNG { get; set; }
        protected LinkedList<HighlightOccurrence> NicknamesOnDelay { get; set; }

        public DontJustHighlightMePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DJHMConfig(config);
            RNG = new Random();
            NicknamesOnDelay = new LinkedList<HighlightOccurrence>();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new DJHMConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (!Config.Channels.Contains(args.Channel))
            {
                // wrong channel
                return;
            }

            ProcessPotentialHighlight(args);
            ProcessPendingRetributions();
        }

        protected void ProcessPotentialHighlight(IChannelMessageEventArgs args)
        {
            string nickLower = args.SenderNickname.ToLowerInvariant();
            string unameLower = ConnectionManager.RegisteredNameForNick(args.SenderNickname)?.ToLowerInvariant();
            if (Config.LowercaseImmuneNicksOrUsernames.Contains(nickLower))
            {
                return;
            }
            if (unameLower != null && Config.LowercaseImmuneNicksOrUsernames.Contains(unameLower))
            {
                return;
            }

            if (Config.NotJustAHighlightRegex.IsMatch(args.Message))
            {
                // not a highlight-only message
                return;
            }

            List<string> potentialNicks = Config.NickDelimiterRegex
                .Split(args.Message)
                .Where(n => n.Length > 0)
                .ToList();
            if (potentialNicks.Count == 0)
            {
                // nope
                return;
            }

            if (potentialNicks.Any(n => !ConnectionManager.IsValidNickname(n)))
            {
                // one of those is not a nick; nevermind
                return;
            }

            var lowercaseChannelUsernames = new HashSet<string>(
                ConnectionManager
                    .NicknamesInChannel(args.Channel)
                    .Select(nick => nick.ToLowerInvariant())
            );

            var lowercaseHighlights = new HashSet<string>();
            foreach (string potentialNick in potentialNicks)
            {
                string lowerPotentialNick = potentialNick.ToLowerInvariant();

                // is it a nickname?
                bool isHighlight = false;
                if (lowercaseChannelUsernames.Contains(lowerPotentialNick))
                {
                    // yes
                    isHighlight = true;
                }
                else
                {
                    // is it an alias?

                    string alias;
                    if (Config.UserAliases.TryGetValue(lowerPotentialNick, out alias))
                    {
                        // yes; is the user in the channel?
                        if (lowercaseChannelUsernames.Contains(alias.ToLowerInvariant()))
                        {
                            // yes
                            isHighlight = true;
                        }
                    }
                }

                if (!isHighlight)
                {
                    // well nevermind then
                    return;
                }

                lowercaseHighlights.Add(potentialNick);
            }

            if (lowercaseHighlights.Count == 0)
            {
                // nobody being highlighted
                return;
            }

            if (lowercaseHighlights.All(lch => lch == args.SenderNickname.ToLowerInvariant()))
            {
                // the user is only naming themselves; never mind
                return;
            }

            // user found; let's see what we will do
            if (Config.TriggerPercentage.HasValue)
            {
                int currentValue = RNG.Next(0, 100);
                if (currentValue >= Config.TriggerPercentage)
                {
                    Logger.LogDebug("RNG decided against triggering");
                    return;
                }
            }

            // calculate delay
            int delay = RNG.Next(Config.DelayMinMessages, Config.DelayMaxMessages + 1);

            // add to list
            NicknamesOnDelay.AddLast(new HighlightOccurrence(args.SenderNickname, lowercaseHighlights.First(), args.Channel, delay));
        }

        protected void ProcessPendingRetributions()
        {
            var current = NicknamesOnDelay.First;

            while (current != null)
            {
                HighlightOccurrence occ = current.Value;

                if (occ.Countdown == 0)
                {
                    // remove it
                    NicknamesOnDelay.Remove(current);

                    if (Config.Kick)
                    {
                        // punt the perpetrator out of the channel
                        Logger.LogDebug(
                            "punting {Perpetrator} from {Channel} for highlighting {Victim}",
                            occ.Perpetrator, occ.Channel, occ.Victim
                        );
                        ConnectionManager.KickChannelUser(occ.Channel, occ.Perpetrator, Config.KickMessage);
                    }
                    else
                    {
                        // highlight the perpetrator as retribution
                        Logger.LogDebug(
                            "re-highlighting {Perpetrator} in {Channel} for highlighting {Victim}",
                            occ.Perpetrator, occ.Channel, occ.Victim
                        );
                        ConnectionManager.SendChannelMessage(occ.Channel, occ.Perpetrator);
                    }
                }
                else
                {
                    --occ.Countdown;
                }

                current = current.Next;
            }
        }
    }
}
