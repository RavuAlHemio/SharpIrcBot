using System;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events.Irc;

namespace Reinvite
{
    public class ReinvitePlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Regex InviteRegex = new Regex("^!invite[ ]+(?<channel>[#&][^ ]{1,256})[ ]*$", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager { get; }
        protected ReinviteConfig Config { get; set; }

        public ReinvitePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new ReinviteConfig(config);
            
            ConnectionManager.QueryMessage += HandleQueryMessage;
            ConnectionManager.Invited += HandleInvite;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new ReinviteConfig(newConfig);
        }

        protected void HandleQueryMessage(object sender, IPrivateMessageEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleQueryMessage(sender, e, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling query message", exc);
            }
        }

        protected void HandleInvite(object sender, IUserInvitedToChannelEventArgs e)
        {
            try
            {
                ActuallyHandleInvite(sender, e);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling invite", exc);
            }
        }

        protected virtual void ActuallyHandleQueryMessage(object sender, IPrivateMessageEventArgs e, MessageFlags flags)
        {
            if (!Config.RejoinOnPrivateMessage)
            {
                return;
            }

            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var match = InviteRegex.Match(e.Message);
            if (!match.Success)
            {
                return;
            }

            var channel = match.Groups["channel"].Value;
            if (Config.AutoJoinedChannelsOnly && !ConnectionManager.AutoJoinChannels.Contains(channel))
            {
                return;
            }

            ConnectionManager.JoinChannel(channel);
        }

        protected void ActuallyHandleInvite(object sender, IUserInvitedToChannelEventArgs e)
        {
            if (!Config.RejoinOnInvite)
            {
                return;
            }

            if (Config.AutoJoinedChannelsOnly && !ConnectionManager.AutoJoinChannels.Contains(e.Channel))
            {
                return;
            }

            ConnectionManager.JoinChannel(e.Channel);
        }
    }
}
