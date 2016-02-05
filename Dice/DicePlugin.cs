using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Dice
{
    public class DicePlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly Regex DiceThrowRegex = new Regex("^!roll +(?<firstDice>[1-9][0-9]*)?d(?<firstSides>[1-9][0-9]*)(?:[, ]+(?<moreDice>[1-9][0-9]*)?d(?<moreSides>[1-9][0-9]*))+[ ]*$", RegexOptions.IgnoreCase);

        protected ConnectionManager ConnectionManager { get; set; }
        protected DiceConfig Config { get; set; }
        protected Random RNG { get; set; }

        public DicePlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DiceConfig(config);
            RNG = new Random();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
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

        protected virtual void ActuallyHandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var match = DiceThrowRegex.Match(args.Data.Message);
            if (!match.Success)
            {
                return;
            }

            var diceGroups = new List<DiceGroup>();
            int? firstDice = SharpIrcBotUtil.MaybeParseInt(match.Groups["firstDice"].Value);
            int? firstSides = SharpIrcBotUtil.MaybeParseInt(match.Groups["firstSides"].Value);
            if (!firstDice.HasValue || firstDice.Value > Config.MaxDiceCount)
            {
                ConnectionManager.SendChannelMessageFormat(args.Data.Channel, "{0}: Too many dice.", args.Data.Nick);
                return;
            }
            if (!firstSides.HasValue || firstSides.Value > Config.MaxSideCount)
            {
                ConnectionManager.SendChannelMessageFormat(args.Data.Channel, "{0}: Too many sides.", args.Data.Nick);
                return;
            }
            diceGroups.Add(new DiceGroup(firstDice.Value, firstSides.Value));

            var moreDiceCaptures = match.Groups["moreDice"].Captures.OfType<Capture>();
            var moreSidesCaptures = match.Groups["moreSides"].Captures.OfType<Capture>();
            foreach (var nextDiceAndSides in moreDiceCaptures.Zip(moreSidesCaptures, Tuple.Create))
            {
                int? nextDice = SharpIrcBotUtil.MaybeParseInt(nextDiceAndSides.Item1.Value);
                int? nextSides = SharpIrcBotUtil.MaybeParseInt(nextDiceAndSides.Item2.Value);
                if (!nextDice.HasValue || nextDice.Value > Config.MaxDiceCount)
                {
                    ConnectionManager.SendChannelMessageFormat(args.Data.Channel, "{0}: Too many dice.", args.Data.Nick);
                    return;
                }
                if (!nextSides.HasValue || nextSides.Value > Config.MaxSideCount)
                {
                    ConnectionManager.SendChannelMessageFormat(args.Data.Channel, "{0}: Too many sides.", args.Data.Nick);
                    return;
                }
                diceGroups.Add(new DiceGroup(nextDice.Value, nextSides.Value));

                if (diceGroups.Count > Config.MaxRollCount)
                {
                    ConnectionManager.SendChannelMessageFormat(args.Data.Channel, "{0}: Too many rolls.", args.Data.Nick);
                    return;
                }
            }

            // special-case 2d1
            if (diceGroups.Count == 1 && diceGroups[0].DieCount == 2 && diceGroups[0].SideCount == 1)
            {
                ConnectionManager.SendChannelAction(args.Data.Channel, "rolls its eyes");
                return;
            }

            var allRolls = new List<string>();
            foreach (var diceGroup in diceGroups)
            {
                var theseRolls = new List<string>(diceGroup.DieCount);
                for (int i = 0; i < diceGroup.DieCount; ++i)
                {
                    if (diceGroup.SideCount == 1 && Config.ObstinateAnswers.Count > 0)
                    {
                        // special case: give an obstinate answer instead since a 1-sided toss has an obvious result
                        string obstinateAnswer = Config.ObstinateAnswers[RNG.Next(Config.ObstinateAnswers.Count)];
                        theseRolls.Add(obstinateAnswer);
                    }
                    else
                    {
                        int roll = RNG.Next(diceGroup.SideCount) + 1;
                        theseRolls.Add(roll.ToString(CultureInfo.InvariantCulture));
                    }
                }
                var theseRollsString = string.Join(" ", theseRolls);
                allRolls.Add(theseRollsString);
            }
            var allRollsString = string.Join("; ", allRolls);

            ConnectionManager.SendChannelMessageFormat(args.Data.Channel, "{0}: {1}", args.Data.Nick, allRollsString);
        }
    }
}
