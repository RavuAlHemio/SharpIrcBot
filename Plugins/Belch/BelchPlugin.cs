using System;
using System.Reflection;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;

namespace Belch
{
    public class BelchPlugin : SharpIrcBot.IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IrcClient Client;

        public BelchPlugin(IrcClient client, JObject config)
        {
            Client = client;
            Client.OnChannelMessage += HandleChannelMessage;
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        private void ActuallyHandleChannelMessage(object sender, IrcEventArgs args)
        {
            var msgArr = args.Data.MessageArray;
            if (msgArr.Length == 1 && string.Equals(msgArr[0], "!belch"))
            {
                Client.SendMessage(SendType.Action, args.Data.Channel, "belches loudly");
            }
        }
    }
}
