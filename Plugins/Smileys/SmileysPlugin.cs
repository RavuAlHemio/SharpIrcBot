using System.Text;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.Smileys
{
    public class SmileysPlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected SmileysConfig Config { get; set; }

        public SmileysPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SmileysConfig(config);

            // it's a fun command in a channel and a useful command in private message
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("smileys", "smilies"),
                    tags: CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleSmileysCommand
            );
            ConnectionManager.CommandManager.RegisterQueryMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("smileys", "smilies"),
                    tags: CommandUtil.NoTags,
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleSmileysCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new SmileysConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleSmileysCommand(CommandMatch cmd, IUserMessageEventArgs e)
        {
            foreach (var smiley in Config.Smileys)
            {
                var smileyLine = new StringBuilder(smiley);
                if (smiley.Length > 1)
                {
                    // escape smiley by adding ZWNBSP in between first and second character

                    smileyLine.Append(" = ");
                    smileyLine.Append(smiley[0]);
                    smileyLine.Append('\uFEFF');
                    smileyLine.Append(smiley, 1, smiley.Length - 1);
                }
                ConnectionManager.SendQueryMessage(e.SenderNickname, smileyLine.ToString());
            }
        }
    }
}

