﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Dice
{
    public class DicePlugin : IPlugin, IReloadableConfiguration
    {
        public static readonly Regex RollRegex = new Regex(
            "(?<dice>[1-9][0-9]*)?" +
            "d" +
            "(?<sides>[1-9][0-9]*)" +
            "(?<addValue>[+-][1-9][0-9]*)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        public static readonly Regex RollSeparatorRegex = new Regex("(?:[,]|\\s)+");

        protected IConnectionManager ConnectionManager { get; set; }
        protected DiceConfig Config { get; set; }
        protected Random RNG { get; set; }
        protected CryptoRandom CryptoRNG { get; set; }
        protected Dictionary<string, CooldownState> ChannelToCooldown { get; set; }

        public DicePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DiceConfig(config);
            RNG = new Random();
            CryptoRNG = new CryptoRandom();
            ChannelToCooldown = new Dictionary<string, CooldownState>();

            ImmutableList<KeyValuePair<string, IArgumentTaker>> cryptoOption =
                CommandUtil.MakeOptions(
                    CommandUtil.MakeFlag("--crypto")
                )
            ;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("roll"),
                    cryptoOption,
                    CommandUtil.MakeArguments(new MultiMatchTaker(RollRegex, RollSeparatorRegex, 1)),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleRollCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("yn"),
                    cryptoOption,
                    CommandUtil.MakeArguments(RestTaker.Instance),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleYesNoCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("decide"),
                    cryptoOption,
                    CommandUtil.MakeArguments(RestTaker.Instance),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleDecideCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("shuffle"),
                    cryptoOption,
                    CommandUtil.MakeArguments(RestTaker.Instance),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleShuffleCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("someyear"),
                    CommandUtil.MakeOptions(
                        CommandUtil.MakeFlag("--crypto"),
                        CommandUtil.MakeOption("-w", CommandUtil.NonzeroStringMatcherRequiredWordTaker),
                        CommandUtil.MakeOption("--wikipedia", CommandUtil.NonzeroStringMatcherRequiredWordTaker)
                    ),
                    CommandUtil.NoArguments,
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleSomeYearCommand
            );
            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new DiceConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected DiceGroup ObtainDiceGroup(Match rollMatch, string channel, string senderNick)
        {
            int? dice = MaybeParseIntGroup(rollMatch.Groups["dice"], defaultValue: 1);
            int? sides = StringUtil.MaybeParseInt(rollMatch.Groups["sides"].Value);
            long? addValue = MaybeParseLongGroup(rollMatch.Groups["addValue"], defaultValue: 0);
            if (!dice.HasValue || dice.Value > Config.MaxDiceCount)
            {
                ConnectionManager.SendChannelMessageFormat(channel, "{0}: Too many dice.", senderNick);
                return null;
            }
            if (!sides.HasValue || sides.Value > Config.MaxSideCount)
            {
                ConnectionManager.SendChannelMessageFormat(channel, "{0}: Too many sides.", senderNick);
                return null;
            }
            if (!addValue.HasValue)
            {
                ConnectionManager.SendChannelMessageFormat(channel, "{0}: Value to add too large.", senderNick);
                return null;
            }
            return new DiceGroup(dice.Value, sides.Value, addValue.Value);
        }

        protected virtual void HandleRollCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            var rolls = (List<Match>)cmd.Arguments[0];
            var diceGroups = new List<DiceGroup>();
            foreach (Match rollMatch in rolls)
            {
                var diceGroup = ObtainDiceGroup(rollMatch, args.Channel, args.SenderNickname);
                if (diceGroup == null)
                {
                    // error occurred and reported; bail out
                    return;
                }
                diceGroups.Add(diceGroup);
            }

            if (diceGroups.Count > Config.MaxRollCount)
            {
                ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: Too many rolls.", args.SenderNickname);
                return;
            }

            // special-case 2d1
            if (diceGroups.Count == 1 && diceGroups[0].DieCount == 2 && diceGroups[0].SideCount == 1 && diceGroups[0].AddValue == 0)
            {
                ConnectionManager.SendChannelAction(args.Channel, "rolls its eyes");
                return;
            }

            Random rng = ChosenRNG(cmd);
            var allRolls = new List<string>();
            foreach (var diceGroup in diceGroups)
            {
                var theseRolls = new List<string>(diceGroup.DieCount);
                for (int i = 0; i < diceGroup.DieCount; ++i)
                {
                    if (diceGroup.SideCount == 1 && Config.ObstinateAnswers.Count > 0)
                    {
                        // special case: give an obstinate answer instead since a 1-sided toss has an obvious result
                        string obstinateAnswer = Config.ObstinateAnswers[rng.Next(Config.ObstinateAnswers.Count)];
                        theseRolls.Add(obstinateAnswer);
                    }
                    else
                    {
                        long roll = rng.Next(diceGroup.SideCount) + 1 + diceGroup.AddValue;
                        theseRolls.Add(roll.ToString(CultureInfo.InvariantCulture));
                    }
                }
                allRolls.Add(theseRolls.StringJoin(" "));
            }

            ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: {1}", args.SenderNickname, allRolls.StringJoin("; "));
        }

        protected virtual bool CheckAndHandleCooldown(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            if (Config.CooldownUpperBoundary < 0)
            {
                // the cooldown feature is not being used
                return false;
            }

            CooldownState cdState;
            if (!ChannelToCooldown.TryGetValue(args.Channel, out cdState))
            {
                cdState = new CooldownState();
                ChannelToCooldown[args.Channel] = cdState;
            }

            cdState.CooldownValue += Config.CooldownPerCommandUsage;

            bool coolingDown = (cdState.CooldownTriggered)
                ? (cdState.CooldownValue > 0)
                : (cdState.CooldownValue > Config.CooldownUpperBoundary);

            if (coolingDown)
            {
                cdState.CooldownTriggered = true;
                string cdAnswer = Config.CooldownAnswers[ChosenRNG(cmd).Next(Config.CooldownAnswers.Count)];
                ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: {1}", args.SenderNickname, cdAnswer);
                return true;
            }

            return false;
        }

        protected virtual void HandleYesNoCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            if (CheckAndHandleCooldown(cmd, args))
            {
                return;
            }

            string yesNoAnswer = Config.YesNoAnswers[ChosenRNG(cmd).Next(Config.YesNoAnswers.Count)];
            ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: {1}", args.SenderNickname, yesNoAnswer);
        }

        protected virtual void HandleDecideCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            if (CheckAndHandleCooldown(cmd, args))
            {
                return;
            }

            string decisionString = ((string)cmd.Arguments[0]).Trim();

            string splitter = Config.DecisionSplitters.FirstOrDefault(ds => decisionString.Contains(ds));
            if (splitter == null)
            {
                ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: Uhh... that looks like only one option to choose from.", args.SenderNickname);
                return;
            }

            Random rng = ChosenRNG(cmd);
            if (Config.SpecialDecisionAnswers.Count > 0)
            {
                int percent = rng.Next(100);
                if (percent < Config.SpecialDecisionAnswerPercent)
                {
                    // special answer instead!
                    var specialAnswer = Config.SpecialDecisionAnswers[rng.Next(Config.SpecialDecisionAnswers.Count)];
                    ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: {1}", args.SenderNickname, specialAnswer);
                    return;
                }
            }

            var options = decisionString.Split(new[] {splitter}, StringSplitOptions.None);
            var chosenOption = options[rng.Next(options.Length)];
            ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: {1}", args.SenderNickname, chosenOption);
        }

        protected virtual void HandleShuffleCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            string decisionString = ((string)cmd.Arguments[0]).Trim();

            string splitter = Config.DecisionSplitters.FirstOrDefault(ds => decisionString.Contains(ds));
            if (splitter == null)
            {
                ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: Uhh... that looks like only one option to shuffle.", args.SenderNickname);
                return;
            }

            Random rng = ChosenRNG(cmd);
            var options = decisionString.Split(new[] {splitter}, StringSplitOptions.None);
            options.Shuffle(rng);
            string newString = string.Join(splitter, options);
            ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: {1}", args.SenderNickname, newString);
        }

        protected virtual void HandleSomeYearCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            string wikiOptionValue = cmd.Options
                .Where(kvp => kvp.Key == "-w" || kvp.Key == "--wikipedia")
                .Select(kvp => (string)kvp.Value)
                .FirstOrDefault() ?? Config.DefaultWikipediaLanguage;

            if (!wikiOptionValue.All(c =>
                (c >= '0' && c <= '9')
                || (c >= 'a' && c <= 'z')
                || (c == '-')
                || (c >= 'A' && c <= 'Z')
            ))
            {
                ConnectionManager.SendChannelMessage(
                    args.Channel,
                    $"{args.SenderNickname}: That does not look like a valid Wikipedia to me."
                );
                return;
            }

            Random rng = ChosenRNG(cmd);
            int year = rng.Next(1, DateTimeOffset.Now.Year + 1);
            ConnectionManager.SendChannelMessage(
                args.Channel,
                $"{args.SenderNickname}: https://{wikiOptionValue}.wikipedia.org/wiki/{year}"
            );
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (Config.CooldownUpperBoundary < 0)
            {
                // the cooldown feature is not being used
                return;
            }

            CooldownState cdState;
            if (!ChannelToCooldown.TryGetValue(args.Channel, out cdState))
            {
                cdState = new CooldownState();
                ChannelToCooldown[args.Channel] = cdState;
            }

            if (cdState.CooldownValue > 0)
            {
                --cdState.CooldownValue;
                if (cdState.CooldownValue == 0)
                {
                    cdState.CooldownTriggered = false;
                }
            }
        }

        protected static int? MaybeParseIntGroup(Group grp, int? defaultValue = null)
        {
            if (!grp.Success)
            {
                return defaultValue;
            }

            return StringUtil.MaybeParseInt(grp.Value, NumberStyles.AllowLeadingSign);
        }

        protected static long? MaybeParseLongGroup(Group grp, long? defaultValue = null)
        {
            if (!grp.Success)
            {
                return defaultValue;
            }

            return StringUtil.MaybeParseLong(grp.Value, NumberStyles.AllowLeadingSign);
        }

        protected virtual Random ChosenRNG(CommandMatch cmd)
        {
            return (cmd.Options.Any(o => o.Key == "--crypto"))
                ? CryptoRNG
                : RNG
            ;
        }
    }
}
