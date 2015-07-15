using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        protected const string CurrentPlayerEventName = "current_player";
        protected const string CurrentPlayerOrderEventName = "current_player_order";
        protected const string TopCardEventName = "current_card";
        protected const string HandInfoEventName = "hand_info";
        protected const string CardCountsEventName = "card_counts";
        protected const string CardDrawnEventName = "player_drew_card";

        protected static readonly Regex UnoBotFirstMessage = new Regex("^([1-9][0-9]*) (.*)");
        protected const string BotCommandRegexPattern = "^([?][a-z]+)[ ]+(?i){0}[ ]*$";

        protected ConnectionManager ConnectionManager;
        protected UnoBotConfig Config;

        protected StringBuilder CurrentMessageJson;
        protected int LinesLeftInMessage;
        protected string BotCommandRegexNick;
        protected Regex BotCommandRegex;

        protected Card TopCard;
        protected List<Card> CurrentHand;
        protected Dictionary<string, int> CurrentCardCounts;
        protected HashSet<string> CurrentPlayers;
        protected string NextPlayer;
        protected string PreviousPlayer;
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
            BotCommandRegexNick = null;
            BotCommandRegex = null;

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandleQueryMessage;

            CurrentHand = new List<Card>();
            CurrentCardCounts = new Dictionary<string, int>();
            CurrentPlayers = new HashSet<string>();
            NextPlayer = null;
            PreviousPlayer = null;
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

        protected virtual bool IsBotCommand(string message, string expectedCommand)
        {
            // ?join
            if (message == expectedCommand)
            {
                return true;
            }

            // ?join MyBot
            var botNick = ConnectionManager.Client.Nickname;
            if (BotCommandRegexNick != botNick)
            {
                BotCommandRegexNick = botNick;
                var pattern = string.Format(BotCommandRegexPattern, Regex.Escape(botNick));
                BotCommandRegex = new Regex(pattern);
            }
            var match = BotCommandRegex.Match(message);
            if (match.Success && string.Equals(match.Groups[1].Value, expectedCommand, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        protected virtual void ActuallyHandleChannelMessage(object sender, IrcEventArgs args)
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

            if (IsBotCommand(message.Message, "?join"))
            {
                ConnectionManager.SendChannelMessage(message.Channel, "!botjoin");

                // don't curse if the number of cards jumps up from 1 to 7 ;)
                LastHandCount = 0;

                return;
            }

            if (IsBotCommand(message.Message, "?leave"))
            {
                ConnectionManager.SendChannelMessage(message.Channel, "!leave");
                return;
            }

            if (message.Message.StartsWith("??color "))
            {
                var denyColor = false;
                if (!CurrentPlayers.Contains(message.Nick))
                {
                    // player is not taking part
                    Logger.DebugFormat("denying {0}'s color request because they are a spectator", message.Nick);
                    denyColor = true;
                }
                if (CurrentCardCounts.Values.All(v => v > Config.PlayToWinThreshold))
                {
                    // everybody has more than two cards
                    Logger.DebugFormat("denying {0}'s color request because everybody has more than {1} cards", message.Nick, Config.PlayToWinThreshold);
                    denyColor = true;
                }
                if (CurrentCardCounts.ContainsKey(message.Nick) && CurrentCardCounts[message.Nick] <= Config.PlayToWinThreshold)
                {
                    // the person who is asking has two cards or less
                    Logger.DebugFormat("denying {0}'s color request because they have {1} cards or fewer ({2})", message.Nick, Config.PlayToWinThreshold, CurrentCardCounts[message.Nick]);
                    denyColor = true;
                }
                if (CurrentHand.Count <= Config.PlayToWinThreshold)
                {
                    // I have two cards or less
                    Logger.DebugFormat("denying {0}'s color request because I have {1} cards or fewer ({2})", message.Nick, Config.PlayToWinThreshold, CurrentHand.Count);
                    denyColor = true;
                }

                if (denyColor)
                {
                    ConnectionManager.SendChannelMessageFormat(message.Channel, "Sorry, {0}, no can do.", message.Nick);
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
                    ConnectionManager.SendChannelMessageFormat(message.Channel, "Yeah, I think that's doable, {0}.", message.Nick);
                }
                else
                {
                    ConnectionManager.SendChannelMessageFormat(message.Channel, "{0}, I'll do my best, but don't count on me...", message.Nick);
                }

                return;
            }
        }

        protected virtual void ActuallyHandleQueryMessage(object sender, IrcEventArgs args)
        {
            var message = args.Data;
            if (message.Nick == ConnectionManager.Client.Nickname)
            {
                return;
            }

            var messageBody = message.Message;

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

            bool playNow = false;
            switch (eventName)
            {
                case CurrentPlayerEventName:
                {
                    // my turn? not my turn? (legacy)
                    var currentPlayer = (string) evt["player"];
                    playNow = (currentPlayer == ConnectionManager.Client.Nickname);
                    break;
                }
                case CurrentPlayerOrderEventName:
                {
                    // my turn? not my turn?
                    var upcomingPlayers = (JArray) evt["order"];
                    playNow = ((string)upcomingPlayers[0] == ConnectionManager.Client.Nickname);
                    NextPlayer = (upcomingPlayers.Count > 1)
                        ? (string)upcomingPlayers[1]
                        : null;
                    // if upcomingPlayers.Count <= 2, then NextPlayer == PreviousPlayer
                    PreviousPlayer = (upcomingPlayers.Count > 2)
                        ? (string)upcomingPlayers.Last
                        : null;
                    CurrentPlayers.Clear();
                    CurrentPlayers.UnionWith(upcomingPlayers.Select(tok => (string)tok));
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
                case CardDrawnEventName:
                {
                    var player = (string)evt["player"];
                    if (player == ConnectionManager.Client.Nickname)
                    {
                        playNow = true;
                    }
                    break;
                }
            }

            if (playNow)
            {
                PlayACard();
            }
        }

        protected virtual void Curse()
        {
            if (Config.Curses.Count == 0)
            {
                return;
            }

            var curse = Config.Curses[Randomizer.Next(Config.Curses.Count)];
            ConnectionManager.SendChannelMessage(Config.UnoChannel, curse);
        }

        protected virtual CardColor PickAColor()
        {
            var colorsToChoose = new List<CardColor>();

            if (ColorRequest.HasValue)
            {
                // we have a pending color request; honor it
                var color = ColorRequest.Value;
                Logger.DebugFormat("honoring color request {0}", color);
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

        protected virtual void PlayColorCard(CardColor color, CardValue value)
        {
            if (color == CardColor.Wild)
            {
                throw new ArgumentOutOfRangeException("color", color, "color must not be Wild");
            }
            if (value == CardValue.Wild || value == CardValue.WildDrawFour)
            {
                throw new ArgumentOutOfRangeException("value", value, "value must be neither Wild nor WildDrawFour");
            }

            ConnectionManager.SendChannelMessageFormat(
                Config.UnoChannel,
                "!play {0} {1}",
                color.ToFullPlayString(),
                value.ToFullPlayString()
            );
        }

        protected virtual void PlayWildCard(CardValue value, CardColor chosenColor)
        {
            if (value != CardValue.Wild && value != CardValue.WildDrawFour)
            {
                throw new ArgumentOutOfRangeException("value", value, "value must be Wild or WildDrawFour");
            }
            if (chosenColor == CardColor.Wild)
            {
                throw new ArgumentOutOfRangeException("chosenColor", chosenColor, "chosenColor must not be Wild");
            }

            ConnectionManager.SendChannelMessageFormat(
                Config.UnoChannel,
                "!play {0} {1}",
                value.ToFullPlayString(),
                chosenColor.ToFullPlayString()
            );
        }

        protected virtual void DrawACard()
        {
            DrewLast = true;
            ConnectionManager.SendChannelMessage(Config.UnoChannel, "!draw");
            return;
        }

        protected virtual void PlayACard()
        {
            var possibleCards = new List<Card>();
            bool nextPickStrategy = true;

            if (Config.DrawAllTheTime && !DrewLast)
            {
                DrawACard();
                return;
            }

            // strategy 1: destroy the next player if they are close to winning
            if (nextPickStrategy && NextPlayer != null && CurrentCardCounts.ContainsKey(NextPlayer))
            {
                Logger.DebugFormat("next player {0} has {1} cards", NextPlayer, CurrentCardCounts[NextPlayer]);

                if (CurrentCardCounts[NextPlayer] <= Config.PlayToWinThreshold)
                {
                    // the player after me has too few cards; try finding an evil card first
                    Logger.Debug("trying to find an evil card");

                    // offensive cards first: D2, WD4
                    possibleCards.AddRange(CurrentHand.Where(hc => hc.Color == TopCard.Color && hc.Value == CardValue.DrawTwo));
                    possibleCards.AddRange(CurrentHand.Where(hc => hc.Value == CardValue.WildDrawFour));

                    if (possibleCards.Count == 0)
                    {
                        // defensive cards next: S, R
                        possibleCards.AddRange(CurrentHand.Where(hc =>
                            hc.Color == TopCard.Color && (
                                hc.Value == CardValue.Skip ||
                                hc.Value == CardValue.Reverse
                            )
                        ));
                    }

                    if (possibleCards.Count > 0)
                    {
                        Logger.Debug("we have an evil card for the next player");

                        // don't add the next pick
                        nextPickStrategy = false;
                    }
                    else if (!DrewLast)
                    {
                        // try drawing
                        Logger.Debug("emergency strategic draw");
                        DrawACard();
                        return;
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

            // strategy 3: pick a card at random
            if (nextPickStrategy)
            {
                // matched only by value, times StandardValueMatchPriority
                var cardsByValue = CurrentHand.Where(hc => hc.Value == TopCard.Value && hc.Color != TopCard.Color).ToList();
                for (int i = 0; i < Config.StandardValueMatchPriority; ++i)
                {
                    possibleCards.AddRange(cardsByValue);
                }

                // matched only by color, times StandardColorMatchPriority
                var cardsByColor = CurrentHand.Where(hc => hc.Color == TopCard.Color && hc.Value != TopCard.Value).ToList();
                for (int i = 0; i < Config.StandardColorMatchPriority; ++i)
                {
                    possibleCards.AddRange(cardsByColor);
                }

                // matched by both color and value, times StandardColorAndValueMatchPriority
                var identicalCards = CurrentHand.Where(hc => hc.Color == TopCard.Color && hc.Value == TopCard.Value).ToList();
                for (int i = 0; i < Config.StandardColorAndValueMatchPriority; ++i)
                {
                    possibleCards.AddRange(identicalCards);
                }

                // color changers (W, WD4), times StandardColorChangePriority
                var colorChangeCards = CurrentHand.Where(hc => hc.Color == CardColor.Wild).ToList();
                for (int i = 0; i < Config.StandardColorChangePriority; ++i)
                {
                    possibleCards.AddRange(colorChangeCards);
                }

                // player sequence reordering cards (R, S, D2, WD4), times StandardReorderPriority
                var reorderCards = CurrentHand.Where(hc =>
                    hc.Value == CardValue.WildDrawFour ||
                    (
                        (
                            hc.Color == TopCard.Color ||
                            hc.Value == TopCard.Value
                        ) && (
                            hc.Value == CardValue.DrawTwo ||
                            hc.Value == CardValue.Reverse ||
                            hc.Value == CardValue.Skip
                        )
                    )
                );
                for (int i = 0; i < Config.StandardReorderPriority; ++i)
                {
                    possibleCards.AddRange(reorderCards);
                }
            }

            // post-strategy filter: if the previous player has too few cards, filter out reverses
            if (PreviousPlayer != null && CurrentCardCounts.ContainsKey(PreviousPlayer) && CurrentCardCounts[PreviousPlayer] <= Config.PlayToWinThreshold)
            {
                Logger.DebugFormat("previous player ({0}) has {1} cards or less ({2}); filtering out reverses", PreviousPlayer, Config.PlayToWinThreshold, CurrentCardCounts[PreviousPlayer]);
                possibleCards.RemoveAll(c => c.Value == CardValue.Reverse);
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
                        Logger.Debug("strategic draw");
                        DrawACard();
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
                    PlayWildCard(card.Value, chosenColor);
                }
                else
                {
                    // play it
                    PlayColorCard(card.Color, card.Value);
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
                if (Config.ManyDrawsCurseThreshold >= 0 && DrawsSinceLastPlay > Config.ManyDrawsCurseThreshold)
                {
                    Logger.Debug("cursing because of too many draws");
                    Curse();
                }
                Logger.Debug("drawing");
                DrawACard();
            }
        }
    }
}
