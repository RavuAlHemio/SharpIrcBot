using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.Belch
{
    public class BelchPlugin : IPlugin
    {
        private static readonly int[] SkittlesCodes = {1, 2, 3, 4, 5, 6, 7, 10, 12, 13};
        protected static readonly Dictionary<char, char> TelNoDictionary = new Dictionary<char, char>
        {
            ['A'] = '2', ['a'] = '2', ['B'] = '2', ['b'] = '2', ['C'] = '2', ['c'] = '2',
            ['D'] = '3', ['d'] = '3', ['E'] = '3', ['e'] = '3', ['F'] = '3', ['f'] = '3',
            ['G'] = '4', ['g'] = '4', ['H'] = '4', ['h'] = '4', ['I'] = '4', ['i'] = '4',
            ['J'] = '5', ['j'] = '5', ['K'] = '5', ['k'] = '5', ['L'] = '5', ['l'] = '5',
            ['M'] = '6', ['m'] = '6', ['N'] = '6', ['n'] = '6', ['O'] = '6', ['o'] = '6',
            ['P'] = '7', ['p'] = '7', ['Q'] = '7', ['q'] = '7', ['R'] = '7', ['r'] = '7', ['S'] = '7', ['s'] = '7',
            ['T'] = '8', ['t'] = '8', ['U'] = '8', ['u'] = '8', ['V'] = '8', ['v'] = '8',
            ['W'] = '9', ['w'] = '9', ['X'] = '9', ['x'] = '9', ['Y'] = '9', ['y'] = '9', ['Z'] = '9', ['z'] = '9',
        };

        protected IConnectionManager ConnectionManager;

        public BelchPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        private void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
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

            if (args.Message.StartsWith("!tel "))
            {
                string telNumber = args.Message.Substring(("!tel ").Length).Trim();
                var ret = new StringBuilder(telNumber.Length);

                foreach (char c in telNumber)
                {
                    char target;
                    if (TelNoDictionary.TryGetValue(c, out target))
                    {
                        ret.Append(target);
                    }
                    else
                    {
                        ret.Append(c);
                    }
                }

                ConnectionManager.SendChannelMessage(args.Channel, ret.ToString());
            }
        }
    }
}
