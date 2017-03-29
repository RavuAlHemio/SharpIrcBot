using System.Text;
using Newtonsoft.Json.Linq;
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

            ConnectionManager.ChannelMessage += HandleChannelOrQueryMessage;
            ConnectionManager.QueryMessage += HandleChannelOrQueryMessage;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new SmileysConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        void HandleChannelOrQueryMessage(object sender, IUserMessageEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (e.Message == "!smileys" || e.Message == "!smilies")
            {
                SendSmileysTo(e.SenderNickname);
            }
        }

        protected virtual void SendSmileysTo(string whom)
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
                ConnectionManager.SendQueryMessage(whom, smileyLine.ToString());
            }
        }
    }
}

