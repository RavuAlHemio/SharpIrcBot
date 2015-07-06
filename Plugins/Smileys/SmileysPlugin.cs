using System;
using System.Text;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Smileys
{
    public class SmileysPlugin : IPlugin
    {
        protected ConnectionManager ConnectionManager;
        protected SmileysConfig Config;

        public SmileysPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SmileysConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelOrQueryMessage;
        }

        void HandleChannelOrQueryMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Message == "!smileys" || e.Data.Message == "!smilies")
            {
                SendSmileysTo(e.Data.Nick);
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

