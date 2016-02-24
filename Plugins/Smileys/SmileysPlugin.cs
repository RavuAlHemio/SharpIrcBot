using System;
using System.Text;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Smileys
{
    public class SmileysPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected ConnectionManager ConnectionManager { get; }
        protected SmileysConfig Config { get; set; }

        public SmileysPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SmileysConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelOrQueryMessage;
            ConnectionManager.QueryMessage += HandleChannelOrQueryMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new SmileysConfig(newConfig);
        }

        void HandleChannelOrQueryMessage(object sender, IrcEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            try
            {
                if (e.Data.Message == "!smileys" || e.Data.Message == "!smilies")
                {
                    SendSmileysTo(e.Data.Nick);
                }
            }
            catch (Exception exc)
            {
                Logger.Error("error while handling channel or query message", exc);
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

