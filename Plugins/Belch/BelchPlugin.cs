using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

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

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("belch"),
                    CommandUtil.NoOptions,
                    CommandUtil.NoArguments,
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleBelchCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("skittles"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(RestTaker.Instance),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleSkittlesCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("tel"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(RestTaker.Instance),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleTelCommand
            );
        }

        protected virtual void HandleBelchCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            ConnectionManager.SendChannelAction(args.Channel, "belches loudly");
        }

        protected virtual void HandleSkittlesCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            const string formatReset = "\x0F";
            string message = ((string)cmd.Arguments[0]);
            if (message.Length > 0)
            {
                // remove leading space
                message = message.Substring(1);
            }
            var currentPiece = new StringBuilder();
            var skittledPieces = new List<string>();
            var colorCodeOffset = new Random().Next(SkittlesCodes.Length);

            string fixedPrefix = $"PRIVMSG {args.Channel} :";
            IEnumerable<(string, int)> charsAndIndexes = StringUtil.StringToCodePointStrings(message)
                .Select((s, i) => (s, i));
            foreach ((string s, int i) in charsAndIndexes)
            {
                int colorCode = SkittlesCodes[(i + colorCodeOffset) % SkittlesCodes.Length];
                string thisCharacter = $"\u0003{colorCode:D2},99{s}";
                int totalIRCLineLength =
                    fixedPrefix.Length
                    + currentPiece.Length
                    + thisCharacter.Length
                    + formatReset.Length;
                if (totalIRCLineLength > ConnectionManager.MaxLineLength)
                {
                    currentPiece.Append(formatReset);
                    skittledPieces.Add(currentPiece.ToString());
                    currentPiece.Clear();
                }
                currentPiece.Append(thisCharacter);
            }

            if (currentPiece.Length > 0)
            {
                // reset formatting
                currentPiece.Append(formatReset);
                skittledPieces.Add(currentPiece.ToString());
            }

            foreach (string piece in skittledPieces)
            {
                ConnectionManager.SendChannelMessage(args.Channel, piece);
            }
        }

        protected virtual void HandleTelCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            string telNumber = ((string)cmd.Arguments[0]);
            if (telNumber.Length > 0)
            {
                // remove leading space
                telNumber = telNumber.Substring(1);
            }
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
