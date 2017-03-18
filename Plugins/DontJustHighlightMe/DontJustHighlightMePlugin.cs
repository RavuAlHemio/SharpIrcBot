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

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new DJHMConfig(newConfig);
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
            var trimmedMessage = args.Message.Trim();
            if (trimmedMessage.Contains(" "))
            {
                // more than one word => no highlight-only message
                return;
            }

            var trimmedLowerMessage = trimmedMessage.ToLowerInvariant();
            string highlightee = null;

            var lowercaseChannelUsernameEnumerable = ConnectionManager
                .NicknamesInChannel(args.Channel)
                .Select(nick => nick.ToLowerInvariant());
            var lowercaseChannelUsernames = new HashSet<string>(lowercaseChannelUsernameEnumerable);

            // is it a username of a user in the list?
            if (lowercaseChannelUsernames.Contains(trimmedLowerMessage))
            {
                // found one!
                Logger.LogDebug("{Message} is a highlight", trimmedLowerMessage);
                highlightee = trimmedLowerMessage;
            }

            if (highlightee == null)
            {
                // is it an alias of a user in the list?
                if (Config.UserAliases.ContainsKey(trimmedLowerMessage))
                {
                    // yes

                    var lowerBaseNick = Config.UserAliases[trimmedLowerMessage].ToLowerInvariant();

                    // is that user currently in the channel?
                    if (lowercaseChannelUsernames.Contains(lowerBaseNick))
                    {
                        // yes
                        Logger.LogDebug("{Message} highlights {Nickname}", trimmedLowerMessage, lowerBaseNick);
                        highlightee = lowerBaseNick;
                    }
                }
            }

            if (highlightee == null)
            {
                // user not found; never mind
                return;
            }

            if (highlightee == args.SenderNickname.ToLowerInvariant())
            {
                // user is naming themselves; never mind
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
            NicknamesOnDelay.AddLast(new HighlightOccurrence(args.SenderNickname, highlightee, args.Channel, delay));
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
