using System;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;

namespace Belch
{
    public class BelchPlugin : SharpIrcBot.IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly int[] SkittlesCodes = {1, 2, 3, 4, 5, 6, 7, 10, 12, 13};

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

            if (msgArr.Length > 1 && msgArr[0] == "!skittles")
            {
                var message = string.Join(" ", msgArr.Skip(1));
                var skittledMessage = new StringBuilder();
                var colorCodeOffset = new Random().Next(SkittlesCodes.Length);

                for (int i = 0; i < message.Length; ++i)
                {
                    int colorCode = SkittlesCodes[(i + colorCodeOffset) % SkittlesCodes.Length];
                    skittledMessage.AppendFormat("\x03{0:D2},99{1}", colorCode, message[i]);
                }
                // reset formatting
                skittledMessage.Append("\x0F");

                Client.SendMessage(SendType.Message, args.Data.Channel, skittledMessage.ToString());
            }
        }
    }
}
