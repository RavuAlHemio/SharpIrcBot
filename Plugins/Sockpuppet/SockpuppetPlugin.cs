using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Sockpuppet
{
    public class SockpuppetPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<SockpuppetPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected SockpuppetConfig Config { get; set; }

        public SockpuppetPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SockpuppetConfig(config);

            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("sockpuppet"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // raw IRC command
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleSockpuppetCommand
            );
            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("reload"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleReloadCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new SockpuppetConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected string VerifyIdentity(IUserMessageEventArgs message)
        {
            var username = message.SenderNickname;
            if (Config.UseIrcServices)
            {
                username = ConnectionManager.RegisteredNameForNick(username);
                if (username == null)
                {
                    Logger.LogInformation("{Nickname} is not logged in; ignoring", message.SenderNickname);
                    return null;
                }
            }

            if (!Config.Puppeteers.Contains(username))
            {
                Logger.LogInformation("{Username} is not a puppeteer; ignoring", username);
                return null;
            }

            return username;
        }

        protected void PerformSockpuppet(string username, string nick, string command)
        {
            var unescapedCommand = StringUtil.UnescapeString(command);
            if (unescapedCommand == null)
            {
                Logger.LogInformation("{Username} bollocksed up their escapes; ignoring", username);
                return;
            }

            Logger.LogInformation("{Username} (nick: {Nickname}) issued the following command: {Command}", username, nick, command);

            ConnectionManager.SendRawCommand(unescapedCommand);
            ConnectionManager.SendQueryMessage(nick, "OK");
        }

        protected virtual void HandleSockpuppetCommand(CommandMatch cmd, IPrivateMessageEventArgs msg)
        {
            string username = VerifyIdentity(msg);
            if (username == null)
            {
                return;
            }

            var command = (string)cmd.Arguments[0];
            if (command.StartsWith(" "))
            {
                command = command.Substring(1);
            }

            PerformSockpuppet(username, msg.SenderNickname, command);
        }

        protected virtual void HandleReloadCommand(CommandMatch cmd, IPrivateMessageEventArgs msg)
        {
            string username = VerifyIdentity(msg);
            if (username == null)
            {
                return;
            }

            ConnectionManager.ReloadConfiguration();
            ConnectionManager.SendQueryMessage(msg.SenderNickname, "OK");
        }
    }
}
