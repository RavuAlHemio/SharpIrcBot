using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Collections;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.CasinoBot.Cards;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    public class CasinoPlayerBotPlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected PlayerConfig Config { get; set; }
        protected EventDispatcher Dispatcher { get; set; }
        protected BlackjackState State { get; set; }
        protected Random Randomizer { get; set; }
        protected ICardCounter CardCounter { get; set; }

        public CasinoPlayerBotPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new PlayerConfig(config);
            Dispatcher = new EventDispatcher();
            State = new BlackjackState();
            Randomizer = new Random();
            CardCounter = TabularCardCounter.Zen.Value;

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandleQueryMessage;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new PlayerConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (args.Channel != Config.CasinoChannel)
            {
                return;
            }

            bool botJoin = false;
            if (args.Message.Trim() == "?botjoin")
            {
                // "?botjoin"
                botJoin = true;
            }
            else
            {
                string[] bits = args.Message.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (bits.Length >= 2 && bits[0] == "?botjoin" && bits.Skip(1).Any(b => b == ConnectionManager.MyNickname))
                {
                    // "?botjoin MyBot" or "?botjoin ThisBot ThatBot MyBot"
                    botJoin = true;
                }
            }

            if (botJoin)
            {
                ConnectionManager.SendChannelMessage(args.Channel, ".botjoin");
            }

            // FIXME: these should be JSON events
            if (args.SenderNickname == Config.GameMasterNickname)
            {
                if (
                    args.Message == "Merging the discards back into the shoe and shuffling..."
                    || args.Message == "The dealer's shoe has been shuffled."
                )
                {
                    CardCounter.ShoeShuffled();
                }
            }
        }

        protected virtual void HandleQueryMessage(object sender, IPrivateMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (!string.Equals(args.SenderNickname, Config.GameMasterNickname, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            JObject eventObject;
            try
            {
                eventObject = JObject.Parse(args.Message);
            }
            catch (JsonReaderException)
            {
                return;
            }

            Dispatcher.DispatchEvent(this, eventObject);
        }

        [Event("turn_info_betting")]
        public virtual void HandleEventTurnInfoBetting([EventValue("player")] string player, [EventValue("stack")] int stack)
        {
            State.Stage = (player == ConnectionManager.MyNickname)
                ? BlackjackStage.MyBetting
                : BlackjackStage.OthersBetting;

            if (State.Stage != BlackjackStage.MyBetting)
            {
                return;
            }

            // my hands are empty again
            State.MyHands.Clear();

            // this is a new round
            State.ActionsTaken = 0;

            // store my current stack size
            State.Stack = stack;

            PlaceBlackjackBet();
        }

        [Event("turn_info")]
        public virtual void HandleEventTurnInfo([EventValue("player")] string player,
                [EventValue("split_round")] int? splitRound = null)
        {
            State.Stage = (player == ConnectionManager.MyNickname)
                ? BlackjackStage.MyTurn
                : BlackjackStage.OthersTurn;

            // warning: assumes order: hand_info -> turn_info -> hand_info -> hand_info -> hand_info ...

            int handIndex = splitRound.HasValue
                ? (splitRound.Value - 1)
                : 0;

            if (State.Stage == BlackjackStage.MyTurn)
            {
                MakeBlackjackMove(handIndex);
            }
        }

        [Event("hand_info")]
        public virtual void HandleEventHandInfo([EventValue("player")] string player,
                [EventValue("hand")] List<Card> hand, [EventValue("split_round")] int? splitRound = null,
                [EventValue("sum")] int? sum = null)
        {
            DistributeHandAssistance(player, hand, splitRound);

            // player's name is "Dealer", player has one card and there is no sum => we're being told the dealer's upcard
            if (hand.Count == 1 && player == "Dealer" && !sum.HasValue)
            {
                State.DealersUpcard = hand.First();
            }

            // irrespective of whose hand (mine, the dealer's or another player's); feed the info to the card counter
            if (State.Stage == BlackjackStage.MyBetting || State.Stage == BlackjackStage.OthersBetting)
            {
                // fresh hands => forward the whole hand
                foreach (Card c in hand)
                {
                    CardCounter.CardDealt(c);
                }
            }
            else
            {
                // only forward the new (last) card
                // this also works for splits, since the first card was dealt previously and the second is new
                if (hand.Count > 0)
                {
                    CardCounter.CardDealt(hand.Last());
                }
            }

            // now then, on to my turn
            if (player != ConnectionManager.MyNickname)
            {
                return;
            }

            int handIndex = splitRound.HasValue
                ? (splitRound.Value - 1)
                : 0;

            // grow additional hands
            while (handIndex >= State.MyHands.Count)
            {
                State.MyHands.Add(null);
            }

            if (State.MyHands[handIndex] == null)
            {
                State.MyHands[handIndex] = new Hand
                {
                    Cards = new SortedMultiset<Card>(hand),
                    Round = 0
                };
            }
            else
            {
                State.MyHands[handIndex].Cards = new SortedMultiset<Card>(hand);
                ++State.MyHands[handIndex].Round;
            }

            GloatOrCurse(handIndex);

            // warning: assumes order: hand_info[0] -> turn_info -> hand_info[1] -> hand_info[2] -> hand_info[3] ...

            if (State.Stage == BlackjackStage.MyTurn)
            {
                MakeBlackjackMove(handIndex);
            }
        }

        [Event("round_end")]
        public virtual void HandleEventRoundEnd()
        {
            State.Stage = BlackjackStage.None;
        }

        protected virtual void PlaceBlackjackBet()
        {
            var bet = (int)Math.Round(Config.BaseBet + CardCounter.BetAdjustment * Config.BetAdjustmentFactor);
            if (bet < Config.MinBet)
            {
                bet = Config.MinBet;
            }
            if (bet > State.Stack)
            {
                bet = State.Stack;
            }
            if (bet > Config.MaxBet)
            {
                bet = Config.MaxBet;
            }

            State.Bet = bet;
            ConnectionManager.SendChannelMessage(Config.CasinoChannel, $".bet {State.Bet}");
        }

        protected virtual void GloatOrCurse(int handIndex)
        {
            Debug.Assert(handIndex >= 0 && handIndex < State.MyHands.Count);
            Hand hand = State.MyHands[handIndex];
            Debug.Assert(hand != null);

            List<int> handValues = hand.Cards
                .BlackjackValues()
                .ToList();

            if (handValues.Contains(BasicStrategy.BlackjackTargetValue))
            {
                // blackjack! gloat
                if (Config.Gloats.Count > 0 && Randomizer.Next(Config.GloatDen) < Config.GloatNum)
                {
                    string gloat = Config.Gloats[Randomizer.Next(Config.Gloats.Count)];
                    ConnectionManager.SendChannelMessage(Config.CasinoChannel, gloat);
                }
            }
            else if (Config.Curses.Count > 0 && handValues.All(v => v > BasicStrategy.BlackjackTargetValue))
            {
                // bust! curse
                if (Randomizer.Next(Config.CurseDen) < Config.CurseNum)
                {
                    string curse = Config.Curses[Randomizer.Next(Config.Curses.Count)];
                    ConnectionManager.SendChannelMessage(Config.CasinoChannel, curse);
                }
            }
        }

        protected virtual void MakeBlackjackMove(int handIndex)
        {
            CourseOfAction? cOA = BasicStrategy.ApplyStrategy(State, handIndex);
            if (!cOA.HasValue)
            {
                return;
            }

            string command = ".stand";

            switch (cOA.Value)
            {
                case CourseOfAction.Hit:
                    command = ".hit";
                    break;
                case CourseOfAction.Stand:
                    command = ".stand";
                    break;
                case CourseOfAction.Split:
                    command = ".split";

                    // wait for the next turn_info, not hand_info
                    State.Stage = BlackjackStage.SplittingMyHand;

                    // remove all hands
                    // FIXME: breaks if resplits are forbidden
                    State.MyHands.Clear();

                    break;
                case CourseOfAction.Surrender:
                    command = ".surrender";
                    break;
                case CourseOfAction.DoubleDown:
                    command = ".doubledown";

                    // I will be told my new hand, but that's not an invitation to play
                    State.Stage = BlackjackStage.OthersTurn;
                    break;
            }

            ConnectionManager.SendChannelMessage(Config.CasinoChannel, command);
            if (handIndex < State.MyHands.Count)
            {
                ++State.MyHands[handIndex].Round;
            }
            ++State.ActionsTaken;
        }

        protected virtual void DistributeHandAssistance(string player, List<Card> hand, int? splitRound = null)
        {
            string handMessage = null;

            foreach (string casinoNick in ConnectionManager.NicknamesInChannel(Config.CasinoChannel))
            {
                string registeredNick = ConnectionManager.RegisteredNameForNick(casinoNick) ?? casinoNick;
                if (!Config.AssistPlayers.Contains(registeredNick))
                {
                    // this player is not interested
                    continue;
                }

                if (handMessage == null)
                {
                    // time to assemble it
                    string playerIdentifier = splitRound.HasValue
                        ? $"{player}-{splitRound.Value}"
                        : player;

                    string[] handValues = hand.BlackjackValues()
                        .OrderBy(v => v)
                        .Select(AnnotateHandValue)
                        .ToArray();

                    string splitChunk = (hand.Count == 2 && hand[0].Value == hand[1].Value)
                        ? "IS SPLITTABLE and "
                        : "";

                    handMessage = $"{playerIdentifier}'s hand {splitChunk}amounts to {string.Join(" or ", handValues)}";
                }

                ConnectionManager.SendQueryNotice(casinoNick, handMessage);
            }
        }

        protected virtual string AnnotateHandValue(int handValue)
        {
            if (handValue <= BasicStrategy.BlackjackSafeMaximum)
            {
                // safe
                return $"{handValue} s";
            }
            if (handValue < BasicStrategy.BlackjackDealerMinimum)
            {
                // under the dealer's minimum
                return $"{handValue} u";
            }
            if (handValue < BasicStrategy.BlackjackTargetValue)
            {
                // duelling the dealer
                return $"{handValue} d";
            }
            if (handValue == BasicStrategy.BlackjackTargetValue)
            {
                // blackjack
                return $"{handValue} !";
            }

            Debug.Assert(handValue > BasicStrategy.BlackjackTargetValue);
            // bust
            return $"{handValue} b";
        }
    }
}
