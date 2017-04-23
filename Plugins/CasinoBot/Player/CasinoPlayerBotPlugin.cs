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
        const int BlackjackSafeMaximum = 11;
        const int BlackjackDealerMinimum = 17;
        const int BlackjackTargetValue = 21;

        protected IConnectionManager ConnectionManager { get; }
        protected PlayerConfig Config { get; set; }
        protected EventDispatcher Dispatcher { get; set; }
        protected BlackjackState State { get; set; }
        protected List<Hand> MyHands { get; set; }
        protected Random Randomizer { get; set; }

        public CasinoPlayerBotPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new PlayerConfig(config);
            Dispatcher = new EventDispatcher();
            State = BlackjackState.None;
            MyHands = new List<Hand>(2);
            Randomizer = new Random();

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
            State = (player == ConnectionManager.MyNickname)
                ? BlackjackState.MyBetting
                : BlackjackState.OthersBetting;

            if (State != BlackjackState.MyBetting)
            {
                return;
            }

            // my hands are empty again
            MyHands.Clear();

            PlaceBlackjackBet();
        }

        [Event("turn_info")]
        public virtual void HandleEventTurnInfo([EventValue("player")] string player,
                [EventValue("split_round")] int? splitRound = null)
        {
            State = (player == ConnectionManager.MyNickname)
                ? BlackjackState.MyTurn
                : BlackjackState.OthersTurn;

            // warning: assumes order: hand_info -> turn_info -> hand_info -> hand_info -> hand_info ...

            int handIndex = splitRound.HasValue
                ? (splitRound.Value - 1)
                : 0;

            if (State == BlackjackState.MyTurn)
            {
                MakeBlackjackMove(handIndex);
            }
        }

        [Event("hand_info")]
        public virtual void HandleEventHandInfo([EventValue("player")] string player,
                [EventValue("hand")] List<Card> hand, [EventValue("split_round")] int? splitRound = null)
        {
            DistributeHandAssistance(player, hand, splitRound);

            if (player != ConnectionManager.MyNickname)
            {
                return;
            }

            int handIndex = splitRound.HasValue
                ? (splitRound.Value - 1)
                : 0;

            // grow additional hands
            while (handIndex >= MyHands.Count)
            {
                MyHands.Add(null);
            }

            if (MyHands[handIndex] == null)
            {
                MyHands[handIndex] = new Hand
                {
                    Cards = new SortedMultiset<Card>(hand),
                    Round = 0
                };
            }
            else
            {
                MyHands[handIndex].Cards = new SortedMultiset<Card>(hand);
                ++MyHands[handIndex].Round;
            }

            // warning: assumes order: hand_info[0] -> turn_info -> hand_info[1] -> hand_info[2] -> hand_info[3] ...

            if (State == BlackjackState.MyTurn)
            {
                MakeBlackjackMove(handIndex);
            }
        }

        [Event("round_end")]
        public virtual void HandleEventRoundEnd()
        {
            State = BlackjackState.None;
        }

        protected virtual void PlaceBlackjackBet()
        {
            // TODO: implement smarter betting strategy
            ConnectionManager.SendChannelMessage(Config.CasinoChannel, ".bet 5");
        }

        protected virtual void MakeBlackjackMove(int handIndex)
        {
            Debug.Assert(handIndex >= 0 && handIndex < MyHands.Count);
            Hand hand = MyHands[handIndex];
            Debug.Assert(hand != null);

            List<int> handValues = hand.Cards.BlackjackValues()
                .ToList();

            // stand immediately if blackjack
            if (handValues.Any(hv => hv == BlackjackTargetValue))
            {
                // aw yiss
                if (Config.Gloats.Count > 0)
                {
                    string gloat = Config.Gloats[Randomizer.Next(Config.Gloats.Count)];
                    ConnectionManager.SendChannelMessage(Config.CasinoChannel, gloat);
                }
                ConnectionManager.SendChannelMessage(Config.CasinoChannel, ".stand");
                return;
            }

            // can we split?
            if (hand.Round == 0 && hand.Cards.Count == 2)
            {
                Card[] bothCards = hand.Cards.ToArray();
                Debug.Assert(bothCards.Length == 2);

                if (bothCards[0].Value == bothCards[1].Value)
                {
                    // yes
                    ConnectionManager.SendChannelMessage(Config.CasinoChannel, ".split");

                    // the game master will now list all our hands; forget the existing ones
                    // (this also ensures that all hands are counted from round 0, allowing additional splits)
                    MyHands.Clear();

                    // don't play upon the next hand_info; wait for the turn_info
                    State = BlackjackState.SplittingMyHand;

                    return;
                }
            }

            int minValue = handValues.Min();
            if (minValue > BlackjackTargetValue)
            {
                // bust
                if (Config.Curses.Count > 0)
                {
                    string curse = Config.Curses[Randomizer.Next(Config.Curses.Count)];
                    ConnectionManager.SendChannelMessage(Config.CasinoChannel, curse);
                }
                return;
            }

            // TODO: implement card counting
            // FIXME: ConnectionManager.SendChannelMessage(Config.CasinoChannel, "there are 52 cards") is not enough

            bool stand = false;
            if (minValue <= BlackjackSafeMaximum)
            {
                stand = false;
            }
            else if (minValue < BlackjackDealerMinimum)
            {
                // low rate
                stand = (Randomizer.Next(0, Config.LowStandDen) < Config.LowStandNum);
            }
            else
            {
                // high rate
                stand = (Randomizer.Next(0, Config.HighStandDen) < Config.HighStandNum);
            }

            ConnectionManager.SendChannelMessage(Config.CasinoChannel, stand ? ".stand" : ".hit");
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
            if (handValue <= BlackjackSafeMaximum)
            {
                // safe
                return $"{handValue} s";
            }
            if (handValue < BlackjackDealerMinimum)
            {
                // under the dealer's minimum
                return $"{handValue} u";
            }
            if (handValue < BlackjackTargetValue)
            {
                // duelling the dealer
                return $"{handValue} d";
            }
            if (handValue == BlackjackTargetValue)
            {
                // blackjack
                return $"{handValue} !";
            }

            Debug.Assert(handValue > BlackjackTargetValue);
            // bust
            return $"{handValue} b";
        }
    }
}
