using System;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;

namespace Belch
{
    public class BelchPlugin : SharpIrcBot.IPlugin
    {
        protected IrcClient Client;

        public BelchPlugin(IrcClient client, JObject config)
        {
            Client = client;
            Client.OnChannelMessage += HandleChannelMessage;
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args)
        {
            var msgArr = args.Data.MessageArray;
            if (msgArr.Length == 1 && string.Equals(msgArr[0], "!belch"))
            {
                Client.SendMessage(SendType.Action, args.Data.Channel, "belches loudly");
            }
        }
    }
}
