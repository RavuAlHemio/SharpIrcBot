﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using SharpIrcBot;

namespace UnoBot
{
    public class UnoBotPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected const string UnoMessagePrefix = "###   ";
        protected const string CurrentPlayerEventName = "current_player";
        protected const string CurrentPlayerOrderEventName = "current_player_order";
        protected const string TopCardEventName = "current_card";
        protected const string HandInfoEventName = "hand_info";
        protected const string CardCountsEventName = "card_counts";

        /// <summary>After the following event is processed (and it's the bot's turn), the bot plays a card.</summary>
        protected const string TriggerPlayEventName = "hand_info";
        protected static readonly Regex UnoBotFirstMessage = new Regex("^([1-9][0-9]*) (.*)");

        protected ConnectionManager ConnectionManager;
        protected UnoBotConfig Config;

        protected StringBuilder CurrentMessageJson;
        protected int LinesLeftInMessage;

        protected bool MyTurn;
        protected Card TopCard;
        protected List<Card> CurrentHand;
        protected Dictionary<string, int> CurrentCardCounts;
        protected string NextPlayer;
        protected int LastHandCount;
        protected CardColor? ColorRequest;
        protected bool DrewLast;
        protected int DrawsSinceLastPlay;
        protected Random Randomizer;

        public UnoBotPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new UnoBotConfig(config);

            CurrentMessageJson = new StringBuilder();
            LinesLeftInMessage = 0;

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandleQueryMessage;

            MyTurn = false;
            CurrentHand = new List<Card>();
            CurrentCardCounts = new Dictionary<string, int>();
            NextPlayer = null;
            LastHandCount = -1;
            ColorRequest = null;
            DrewLast = false;
            DrawsSinceLastPlay = 0;
            Randomizer = new Random();
        }

        public static string StripColors(string str)
        {
            var ret = new StringBuilder(str.Length);
            int skippingColorStage = 0;
            foreach (char c in str)
            {
                if (skippingColorStage > 0)
                {
                    if (c >= '0' && c <= '9')
                    {
                        continue;
                    }
                    else if (skippingColorStage == 1 && c == ',')
                    {
                        skippingColorStage = 2;
                        continue;
                    }
                    else
                    {
                        skippingColorStage = 0;
                        // fall through
                    }
                }

                if (c == 0x0F)
                {
                    // switch back to plain
                    continue;
                }
                else if (c == 0x02)
                {
                    // bold
                    continue;
                }
                else if (c == 0x1D)
                {
                    // italics
                    continue;
                }
                else if (c == 0x1F)
                {
                    // underline
                    continue;
                }
                else if (c == 0x16)
                {
                    // reverse
                    continue;
                }
                else if (c == 0x03)
                {
                    // color
                    skippingColorStage = 1;
                    continue;
                }

                // append
                ret.Append(c);
            }

            return ret.ToString();
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

        private void HandleQueryMessage(object sender, IrcEventArgs args)
        {
            try
            {
                ActuallyHandleQueryMessage(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling query message", exc);
            }
        }

        protected void ActuallyHandleChannelMessage(object sender, IrcEventArgs args)
        {
            var message = args.Data;
            if (message.Type != ReceiveType.ChannelMessage || message.Nick == ConnectionManager.Client.Nickname)
            {
                return;
            }

            if (Config.UnoChannel != message.Channel)
            {
                return;
            }

            if (message.Message == "??join")
            {
                ConnectionManager.SendChannelMessage(message.Channel, "!botjoin");

                // don't curse if the number of cards jumps up from 1 to 7 ;)
                LastHandCount = 0;

                return;
            }

            if (message.Message == "??leave")
            {
                ConnectionManager.SendChannelMessage(message.Channel, "!leave");
                return;
            }

            if (message.Message.StartsWith("??color "))
            {
                var denyColor = false;
                if (CurrentCardCounts.Values.All(v => v > Config.PlayToWinThreshold))
                {
                    // everybody has more than two cards
                    denyColor = true;
                }
                if (CurrentCardCounts.ContainsKey(message.Nick) && CurrentCardCounts[message.Nick] <= Config.PlayToWinThreshold)
                {
                    // the person who is asking has two cards or less
                    denyColor = true;
                }
                if (CurrentHand.Count <= Config.PlayToWinThreshold)
                {
                    // I have two cards or less
                    denyColor = true;
                }

                if (denyColor)
                {
                    ConnectionManager.SendChannelMessage(message.Channel, "Sorry, no can do.");
                    return;
                }

                var colorString = message.Message.Substring(("??color ").Length);
                var color = CardUtils.ParseColor(colorString);
                if (!color.HasValue || color == CardColor.Wild)
                {
                    ConnectionManager.SendChannelMessage(message.Channel, "Uhh, what color is that?");
                    return;
                }

                ColorRequest = color;

                // can I change the color?
                if (CurrentHand.Any(c => c.Color == color || c.Color == CardColor.Wild))
                {
                    ConnectionManager.SendChannelMessage(message.Channel, "Yeah, I think that's doable.");
                }
                else
                {
                    ConnectionManager.SendChannelMessage(message.Channel, "I'll do my best, but don't count on me...");
                }

                return;
            }
        }

        protected void ActuallyHandleQueryMessage(object sender, IrcEventArgs args)
        {
            var message = args.Data;
            if (message.Nick == ConnectionManager.Client.Nickname)
            {
                return;
            }

            if (!message.Message.StartsWith(UnoMessagePrefix))
            {
                return;
            }

            var messageBody = message.Message.Substring(UnoMessagePrefix.Length);

            if (LinesLeftInMessage > 0)
            {
                // add this
                CurrentMessageJson.Append(messageBody);
                --LinesLeftInMessage;
            }
            else
            {
                var match = UnoBotFirstMessage.Match(messageBody);
                if (!match.Success)
                {
                    // nope
                    return;
                }

                LinesLeftInMessage = int.Parse(match.Groups[1].Value);
                CurrentMessageJson.Append(match.Groups[2].Value);
                --LinesLeftInMessage;
            }

            if (LinesLeftInMessage > 0)
            {
                // wait for more
                return;
            }
            
            // ready to parse
            var parseMe = CurrentMessageJson.ToString();
            CurrentMessageJson.Clear();

            var evt = JObject.Parse(parseMe);
            var eventName = (string) evt["event"];

            Logger.DebugFormat("received event {0}", eventName);

            switch (eventName)
            {
                case CurrentPlayerEventName:
                {
                    // my turn? not my turn? (legacy)
                    var currentPlayer = (string) evt["player"];
                    MyTurn = (currentPlayer == ConnectionManager.Client.Nickname);
                    break;
                }
                case CurrentPlayerOrderEventName:
                {
                    // my turn? not my turn?
                    var upcomingPlayers = (JArray) evt["order"];
                    MyTurn = ((string)upcomingPlayers[0] == ConnectionManager.Client.Nickname);
                    NextPlayer = (upcomingPlayers.Count > 1)
                        ? (string)upcomingPlayers[1]
                        : null;
                    break;
                }
                case CardCountsEventName:
                {
                    var cardCounts = (JArray) evt["counts"];
                    CurrentCardCounts.Clear();
                    foreach (JObject playerAndCount in cardCounts)
                    {
                        var player = (string) playerAndCount["player"];
                        var count = (int) playerAndCount["count"];
                        CurrentCardCounts[player] = count;
                    }
                    break;
                }
                case TopCardEventName:
                {
                    var currentCardName = (string) evt["current_card"];
                    TopCard = CardUtils.ParseColorAndValue(currentCardName).Value;
                    break;
                }
                case HandInfoEventName:
                {
                    var handCards = (JArray) evt["hand"];
                    CurrentHand = handCards
                        .Select(e => CardUtils.ParseColorAndValue((string)e))
                        .Where(cav => cav.HasValue)
                        .Select(cav => cav.Value)
                        .ToList();
                    if (LastHandCount > 0 && Config.ManyCardsCurseThreshold > 0 && CurrentHand.Count - LastHandCount >= Config.ManyCardsCurseThreshold)
                    {
                        Logger.Debug("cursing because of overfilled hand");
                        Curse();
                    }
                    LastHandCount = CurrentHand.Count;
                    break;
                }
            }

            if (eventName == TriggerPlayEventName && MyTurn)
            {
                PlayACard();
            }
        }

        protected void Curse()
        {
            if (Config.Curses.Count == 0)
            {
                return;
            }

            var curse = Config.Curses[Randomizer.Next(Config.Curses.Count)];
            ConnectionManager.SendChannelMessage(Config.UnoChannel, curse);
        }

        protected CardColor PickAColor()
        {
            var colorsToChoose = new List<CardColor>();

            if (ColorRequest.HasValue)
            {
                // we have a pending color request; honor it
                var color = ColorRequest.Value;
                ColorRequest = null;
                return color;
            }

            // -> add all four colors once to allow for some chaotic color switching
            colorsToChoose.Add(CardColor.Red);
            colorsToChoose.Add(CardColor.Green);
            colorsToChoose.Add(CardColor.Blue);
            colorsToChoose.Add(CardColor.Yellow);

            // -> add all the (non-wild) colors from our hand to increase the chances of a useful pick
            colorsToChoose.AddRange(CurrentHand.Select(c => c.Color).Where(c => c != CardColor.Wild));

            // -> choose at random
            return colorsToChoose[Randomizer.Next(colorsToChoose.Count)];
        }

        protected void PlayACard()
        {
            var possibleCards = new List<Card>();
            bool nextPickStrategy = true;

            // strategy 1: destroy the next player if they are close to winning
            if (nextPickStrategy && NextPlayer != null && CurrentCardCounts.ContainsKey(NextPlayer))
            {
                Logger.DebugFormat("next player {0} has {1} cards", NextPlayer, CurrentCardCounts[NextPlayer]);

                if (CurrentCardCounts[NextPlayer] <= Config.PlayToWinThreshold)
                {
                    // the player after me has too few cards; try finding an evil card first
                    Logger.Debug("trying to find an evil card");

                    // D2, S, R
                    possibleCards.AddRange(CurrentHand.Where(hc =>
                        hc.Color == TopCard.Color && (
                            hc.Value == CardValue.DrawTwo ||
                            hc.Value == CardValue.Skip ||
                            hc.Value == CardValue.Reverse
                        )
                    ));

                    // WD4
                    possibleCards.AddRange(CurrentHand.Where(hc => hc.Value == CardValue.WildDrawFour));

                    if (possibleCards.Count > 0)
                    {
                        Logger.Debug("we have an evil card for the next player");

                        // don't add the next pick
                        nextPickStrategy = false;
                    }
                }
            }

            // strategy 2: honor color requests
            if (nextPickStrategy && ColorRequest.HasValue)
            {
                if (TopCard.Color == ColorRequest.Value)
                {
                    // glad that's been taken care of
                    ColorRequest = null;
                }
                else
                {
                    // do I have a usable card that matches the target color?
                    possibleCards.AddRange(CurrentHand.Where(hc => hc.Color == ColorRequest.Value && hc.Value == TopCard.Value));
                    if (possibleCards.Count == 0)
                    {
                        // nope; try changing with a wild card instead
                        possibleCards.AddRange(CurrentHand.Where(hc => hc.Color == CardColor.Wild));
                    }
                    if (possibleCards.Count > 0)
                    {
                        // alright, no need for the standard pick
                        nextPickStrategy = false;
                    }
                }
            }

            // strategy: pick a card at random
            if (nextPickStrategy)
            {
                // by value, times three
                var cardsByValue = CurrentHand.Where(hc => hc.Value == TopCard.Value).ToList();
                possibleCards.AddRange(cardsByValue);
                possibleCards.AddRange(cardsByValue);
                possibleCards.AddRange(cardsByValue);

                // then by color, times two
                var cardsByColor = CurrentHand.Where(hc => hc.Color == TopCard.Color).ToList();
                possibleCards.AddRange(cardsByColor);
                possibleCards.AddRange(cardsByColor);

                // then wildcards, times one
                possibleCards.AddRange(CurrentHand.Where(hc => hc.Color == CardColor.Wild));
            }

            if (possibleCards.Count > 0)
            {
                // pick one at random
                var index = Randomizer.Next(possibleCards.Count);
                var card = possibleCards[index];

                // if more than two cards in hand, perform a strategic draw 10% of the time
                if (CurrentHand.Count > Config.PlayToWinThreshold && !DrewLast)
                {
                    var strategicDraw = (Randomizer.Next(10) == 0);
                    if (strategicDraw)
                    {
                        DrewLast = true;
                        Logger.Debug("strategic draw");
                        ConnectionManager.SendChannelMessage(Config.UnoChannel, "!draw");
                        return;
                    }
                }

                Logger.DebugFormat("playing card: {0} {1}", card.Color, card.Value);

                if (card.Color == CardColor.Wild)
                {
                    // pick a color
                    var chosenColor = PickAColor();
                    Logger.DebugFormat("chosen color: {0}", chosenColor);

                    // play the card
                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "!p {0} {1}",
                        card.Value.ToPlayString(),
                        chosenColor.ToPlayString()
                    );
                }
                else
                {
                    // play it
                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "!p {0} {1}",
                        card.Color.ToPlayString(),
                        card.Value.ToPlayString()
                    );
                }
                DrewLast = false;
                DrawsSinceLastPlay = 0;
                return;
            }

            if (DrewLast)
            {
                DrewLast = false;
                ++DrawsSinceLastPlay;
                Logger.Debug("passing");
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "!pass");
            }
            else
            {
                DrewLast = true;
                Logger.Debug("drawing");
                if (Config.ManyDrawsCurseThreshold >= 0 && DrawsSinceLastPlay > Config.ManyDrawsCurseThreshold)
                {
                    Logger.Debug("cursing because of too many draws");
                    Curse();
                }
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "!draw");
            }
        }
    }
}
