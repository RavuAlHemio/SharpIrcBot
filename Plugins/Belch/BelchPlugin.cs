using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Belch
{
    public class BelchPlugin : SharpIrcBot.IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly int[] SkittlesCodes = {1, 2, 3, 4, 5, 6, 7, 10, 12, 13};

        protected ConnectionManager ConnectionManager;

        public BelchPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            ConnectionManager.ChannelMessage += HandleChannelMessage;
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
                ConnectionManager.SendChannelAction(args.Data.Channel, "belches loudly");
            }

            if (msgArr.Length > 1 && msgArr[0] == "!skittles")
            {
                const string formatReset = "\x0F";
                var message = string.Join(" ", msgArr.Skip(1));
                var currentPiece = new StringBuilder();
                var skittledPieces = new List<string>();
                var colorCodeOffset = new Random().Next(SkittlesCodes.Length);

                for (int i = 0; i < message.Length; ++i)
                {
                    int colorCode = SkittlesCodes[(i + colorCodeOffset) % SkittlesCodes.Length];
                    var thisCharacter = string.Format("\x03{0:D2},99{1}", colorCode, message[i]);
                    if (currentPiece.Length + thisCharacter.Length + formatReset.Length > ConnectionManager.MaxMessageLength)
                    {
                        currentPiece.Append(formatReset);
                        skittledPieces.Add(currentPiece.ToString());
                        currentPiece.Clear();
                    }
                    currentPiece.AppendFormat("\x03{0:D2},99{1}", colorCode, message[i]);
                }
                // reset formatting
                currentPiece.Append(formatReset);
                skittledPieces.Add(currentPiece.ToString());

                foreach (var piece in skittledPieces)
                {
                    ConnectionManager.SendChannelMessage(args.Data.Channel, piece);
                }
            }
        }
    }
}
