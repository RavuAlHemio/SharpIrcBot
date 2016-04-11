using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events.Irc;

namespace DontJustHighlightMe
{
    public class DontJustHighlightMePlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IConnectionManager ConnectionManager { get; set; }
        protected DJHMConfig Config { get; set; }
        protected Random RNG { get; set; }

        public DontJustHighlightMePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DJHMConfig(config);
            RNG = new Random();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new DJHMConfig(newConfig);
        }

        private void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
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

        protected virtual void ActuallyHandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (!Config.Channels.Contains(args.Channel))
            {
                // wrong channel
                return;
            }

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
                Logger.DebugFormat("{0} is a highlight", trimmedLowerMessage);
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
                        Logger.DebugFormat("{0} highlights {1}", trimmedLowerMessage, lowerBaseNick);
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
                    Logger.Debug("RNG decided against triggering");
                    return;
                }
            }

            if (Config.Kick)
            {
                // punt the perpetrator out of the channel
                Logger.DebugFormat(
                    "punting {0} from {1} for highlighting {2}",
                    args.SenderNickname,
                    args.Channel,
                    highlightee
                );
                ConnectionManager.KickChannelUser(args.Channel, args.SenderNickname, Config.KickMessage);
            }
            else
            {
                // highlight the perpetrator as retribution
                Logger.DebugFormat(
                    "re-highlighting {0} in {1} for highlighting {2}",
                    args.SenderNickname,
                    args.Channel,
                    highlightee
                );
                ConnectionManager.SendChannelMessage(args.Channel, args.SenderNickname);
            }
        }
    }
}
