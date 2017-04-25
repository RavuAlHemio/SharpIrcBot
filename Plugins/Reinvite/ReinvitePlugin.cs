using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.Reinvite
{
    public class ReinvitePlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected ReinviteConfig Config { get; set; }

        public ReinvitePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new ReinviteConfig(config);

            ConnectionManager.Invited += HandleInvite;

            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("invite"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        new RegexMatcher("^[#&]\\S{1,256}").ToRequiredWordTaker() // channel name
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleInviteCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new ReinviteConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleInviteCommand(CommandMatch cmd, IPrivateMessageEventArgs e)
        {
            if (!Config.RejoinOnPrivateMessage)
            {
                return;
            }

            var channel = ((Match)cmd.Arguments[0]).Value;
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
