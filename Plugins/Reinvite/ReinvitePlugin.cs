using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.Reinvite
{
    public class ReinvitePlugin : IPlugin, IReloadableConfiguration
    {
        public static readonly Regex InviteRegex = new Regex("^!invite\\s+(?<channel>[#&]\\S{1,256})\\s*$", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager { get; }
        protected ReinviteConfig Config { get; set; }

        public ReinvitePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new ReinviteConfig(config);
            
            ConnectionManager.QueryMessage += HandleQueryMessage;
            ConnectionManager.Invited += HandleInvite;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new ReinviteConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleQueryMessage(object sender, IPrivateMessageEventArgs e, MessageFlags flags)
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

        protected void HandleInvite(object sender, IUserInvitedToChannelEventArgs e)
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
