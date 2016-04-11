using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events.Irc;

namespace Belch
{
    public class BelchPlugin : SharpIrcBot.IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly int[] SkittlesCodes = {1, 2, 3, 4, 5, 6, 7, 10, 12, 13};

        protected IConnectionManager ConnectionManager;

        public BelchPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        private void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        private void ActuallyHandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (string.Equals(args.Message, "!belch"))
            {
                ConnectionManager.SendChannelAction(args.Channel, "belches loudly");
            }

            if (args.Message.StartsWith("!skittles "))
            {
                const string formatReset = "\x0F";
                var message = args.Message.Substring(("!skittles ").Length);
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
                    ConnectionManager.SendChannelMessage(args.Channel, piece);
                }
            }
        }
    }
}
