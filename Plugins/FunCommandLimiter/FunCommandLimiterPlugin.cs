using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.FunCommandLimiter
{
    /// <summary>
    /// Limit the number of "fun commands" being used in a channel per unit of time.
    /// </summary>
    public class FunCommandLimiterPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<FunCommandLimiterPlugin>();
        
        public const string GlobalChannelKey = "\r\nGLOBAL";
        public const string PrivateMessageChannelKey = "\r\nQUERY";
        public const string GlobalUserKey = "\r\nGLOBAL";

        protected IConnectionManager Connection { get; set; }
        protected FCLConfig Config { get; set; }
        protected Dictionary<string, Dictionary<string, List<DateTimeOffset>>> ChannelToUserToTimestamps { get; set; }

        public FunCommandLimiterPlugin(IConnectionManager connMgr, JObject config)
        {
            Connection = connMgr;
            Config = new FCLConfig(config);
            ChannelToUserToTimestamps = new Dictionary<string, Dictionary<string, List<DateTimeOffset>>>();

            Connection.CommandManager.RegisterGlobalCommandCallback(LimiterCallback);
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new FCLConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual bool LimiterCallback(CommandMatch commandMatch, IUserMessageEventArgs msg)
        {
            // is it a fun command?
            if (!commandMatch.Command.Tags.Any(tag => Config.CommandTags.Contains(tag)))
            {
                // no => don't protest
                return true;
            }

            // how soon is then?
            DateTimeOffset cutoff = DateTimeOffset.Now.AddMinutes(-Config.TimeSpanMinutes);

            // find the keys
            string channelKey;
            if (Config.PerChannel)
            {
                var chanMsg = msg as IChannelMessageEventArgs;
                if (chanMsg != null)
                {
                    if (!Config.Channels.Contains(chanMsg.Channel))
                    {
                        // we're not policing this channel
                        return true;
                    }

                    channelKey = chanMsg.Channel;
                }
                else
                {
                    // private message
                    if (!Config.CountPrivateMessages)
                    {
                        // we don't bother with those
                        return true;
                    }

                    channelKey = PrivateMessageChannelKey;
                }
            }
            else
            {
                if (msg is IPrivateMessageEventArgs && !Config.CountPrivateMessages)
                {
                    // we don't bother with private messages
                    return true;
                }

                channelKey = GlobalChannelKey;
            }

            string userKey = Config.PerUser
                ? (Connection.RegisteredNameForNick(msg.SenderNickname) ?? msg.SenderNickname)
                : GlobalUserKey;

            // obtain the timestamps
            List<DateTimeOffset> timestamps = ObtainTimeStampsForChannelAndUserKeys(channelKey, userKey);

            // clear out the old ones
            timestamps.RemoveAll(ts => ts < cutoff);

            if (timestamps.Count > Config.MaxCountPerTime)
            {
                // too many

                if (Config.CountLimitedAttempts)
                {
                    // count this one even though it failed >;-)
                    timestamps.Add(DateTimeOffset.Now);
                }

                return false;
            }

            // count this one but allow it
            timestamps.Add(DateTimeOffset.Now);
            return true;
        }

        protected List<DateTimeOffset> ObtainTimeStampsForChannelAndUserKeys(string channelKey, string userKey)
        {
            Dictionary<string, List<DateTimeOffset>> userToTimestamps;
            if (!ChannelToUserToTimestamps.TryGetValue(channelKey, out userToTimestamps))
            {
                userToTimestamps = new Dictionary<string, List<DateTimeOffset>>();
                ChannelToUserToTimestamps[channelKey] = userToTimestamps;
            }

            List<DateTimeOffset> timestamps;
            if (!userToTimestamps.TryGetValue(userKey, out timestamps))
            {
                timestamps = new List<DateTimeOffset>();
                userToTimestamps[userKey] = timestamps;
            }

            return timestamps;
        }
    }
}
