using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using SharpIrcBot;
using UnoBot.RuntimeTweaking;

namespace UnoBot
{
    public class UnoBotPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog CommunicationLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType.FullName + ".Communication");
        private static readonly ILog StrategyLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType.FullName + ".Strategy");

        protected delegate StrategyContinuation StrategyFunction(List<Card> possibleCards);
        protected delegate void FilterFunction(List<Card> possibleCards);
        
        protected const string CurrentPlayerEventName = "current_player";
        protected const string CurrentPlayerOrderEventName = "current_player_order";
        protected const string TopCardEventName = "current_card";
        protected const string HandInfoEventName = "hand_info";
        protected const string CardCountsEventName = "card_counts";
        protected const string CardDrawnEventName = "player_drew_card";

        protected static readonly Regex UnoBotFirstMessage = new Regex("^([1-9][0-9]*) (.*)");
        protected const string BotCommandRegexPattern = "^([?][a-z]+)[ ]+(?i){0}[ ]*$";
        protected static readonly Regex RuntimeTweakPattern = new Regex("^!unobotset[ ]+([A-Za-z]+)[ ]+(.+)$");

        protected ConnectionManager ConnectionManager { get; }
        protected UnoBotConfig Config { get; set; }

        protected StringBuilder CurrentMessageJson { get; }
        protected int LinesLeftInMessage { get; set; }
        protected string BotCommandRegexNick { get; set; }
        protected Regex BotCommandRegex { get; set; }

        protected Card TopCard { get; set; }
        protected List<Card> CurrentHand { get; set; }
        protected Dictionary<string, int> CurrentCardCounts { get; }
        protected HashSet<string> CurrentPlayers { get; }
        protected string NextPlayer { get; set; }
        protected string NextButOnePlayer { get; set; }
        protected string PreviousPlayer { get; set; }
        protected int LastHandCount { get; set; }
        protected CardColor? ColorRequest { get; set; }
        protected bool DrewLast { get; set; }
        protected int DrawsSinceLastPlay { get; set; }
        protected Random Randomizer { get; }

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
            NextButOnePlayer = null;
            PreviousPlayer = null;
            LastHandCount = -1;
            ColorRequest = null;
            DrewLast = false;
            DrawsSinceLastPlay = 0;
            Randomizer = new Random();
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new UnoBotConfig(newConfig);
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

        private void HandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                CommunicationLogger.Error("error handling message", exc);
            }
        }

        private void HandleQueryMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleQueryMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                CommunicationLogger.Error("error handling query message", exc);
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

        protected virtual void ActuallyHandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

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
                    StrategyLogger.DebugFormat("denying {0}'s color request because they are a spectator", message.Nick);
                    denyColor = true;
                }
                if (CurrentCardCounts.Values.All(v => v > Config.PlayToWinThreshold))
                {
                    // everybody has more than two cards
                    StrategyLogger.DebugFormat("denying {0}'s color request because everybody has more than {1} cards", message.Nick, Config.PlayToWinThreshold);
                    denyColor = true;
                }
                if (CurrentCardCounts.ContainsKey(message.Nick) && CurrentCardCounts[message.Nick] <= Config.PlayToWinThreshold)
                {
                    // the person who is asking has two cards or less
                    StrategyLogger.DebugFormat("denying {0}'s color request because they have {1} cards or fewer ({2})", message.Nick, Config.PlayToWinThreshold, CurrentCardCounts[message.Nick]);
                    denyColor = true;
                }
                if (CurrentHand.Count <= Config.PlayToWinThreshold)
                {
                    // I have two cards or less
                    StrategyLogger.DebugFormat("denying {0}'s color request because I have {1} cards or fewer ({2})", message.Nick, Config.PlayToWinThreshold, CurrentHand.Count);
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

            var runtimeTweakMatch = RuntimeTweakPattern.Match(message.Message);
            if (runtimeTweakMatch.Success)
            {
                if (!Config.RuntimeTweakable)
                {
                    ConnectionManager.SendChannelMessageFormat(message.Channel, "Sorry, {0}, I'm not allowed to do that.", message.Nick);
                    return;
                }

                try
                {
                    ConfigTweaking.TweakConfig(Config, runtimeTweakMatch.Groups[1].Value, runtimeTweakMatch.Groups[2].Value);
                }
                catch (ArgumentException ae)
                {
                    ConnectionManager.SendChannelMessageFormat(message.Channel, "That didn't work out, {0}: {1}", message.Nick, ae.Message);
                    return;
                }
                ConnectionManager.SendChannelMessageFormat(message.Channel, "{0}: Done.", message.Nick);

                return;
            }
        }

        protected virtual void ActuallyHandleQueryMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

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
            ProcessJsonEvent(evt);
        }

        protected virtual void ProcessJsonEvent(JObject evt)
        {
            var eventName = (string) evt["event"];

            CommunicationLogger.DebugFormat("received event {0}", eventName);

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
                    // if upcomingPlayers.Count <= 2, then NextButOnePlayer == me
                    NextButOnePlayer = (upcomingPlayers.Count > 2)
                        ? (string)upcomingPlayers[2]
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
                        StrategyLogger.Debug("cursing because of overfilled hand");
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
                StrategyLogger.DebugFormat("honoring color request {0}", color);
                ColorRequest = null;
                return color;
            }

            // -> add all four colors once to allow for some chaotic color switching
            colorsToChoose.Add(CardColor.Red);
            colorsToChoose.Add(CardColor.Green);
            colorsToChoose.Add(CardColor.Blue);
            colorsToChoose.Add(CardColor.Yellow);

            // -> add all the (non-wild) colors from our hand to increase the chances of a useful pick
            var handColors = CurrentHand.Select(c => c.Color).Where(c => c != CardColor.Wild).ToList();
            for (int i = 0; i < Config.ColorInHandPreference; ++i)
            {
                colorsToChoose.AddRange(handColors);
            }

            // -> choose at random
            return colorsToChoose[Randomizer.Next(colorsToChoose.Count)];
        }

        protected virtual void PlayColorCard(Card card)
        {
            PlayColorCard(card.Color, card.Value);
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
        }

        protected virtual void PassAfterDrawing()
        {
            ++DrawsSinceLastPlay;
            DrewLast = false;
            ConnectionManager.SendChannelMessage(Config.UnoChannel, "!pass");
        }

        /// <summary>
        /// Strategy: try to destroy the next player if they are close to winning.
        /// </summary>
        protected StrategyContinuation StrategyDestroyNextPlayerIfDangerous(List<Card> possibleCards)
        {
            if (NextPlayer == null || !CurrentCardCounts.ContainsKey(NextPlayer))
            {
                // not sure who my next player is or how many cards they have
                return StrategyContinuation.ContinueToNextStrategy;
            }

            StrategyLogger.DebugFormat("next player {0} has {1} cards", NextPlayer, CurrentCardCounts[NextPlayer]);

            if (CurrentCardCounts[NextPlayer] > Config.PlayToWinThreshold)
            {
                // not dangerous
                return StrategyContinuation.ContinueToNextStrategy;
            }

            // the player after me has too few cards; try finding an evil card first
            StrategyLogger.Debug("trying to find an evil card");

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
                StrategyLogger.DebugFormat("we have at least one evil card for the next player ({0})", string.Join(", ", possibleCards));

                // don't add the next pick
                return StrategyContinuation.SkipAllOtherStrategies;
            }
            else if (!DrewLast)
            {
                if (CurrentHand.Count <= Config.PlayToWinThreshold)
                {
                    StrategyLogger.DebugFormat("not risking emergency strategic draw for evil card");
                }
                else if (Config.EmergencyStrategicDrawDenominator > 0)
                {
                    var emergencyStrategicDraw = (Randomizer.Next(Config.EmergencyStrategicDrawDenominator) == 0);
                    if (emergencyStrategicDraw)
                    {
                        StrategyLogger.Debug("emergency strategic draw for evil card");
                        DrawACard();
                        return StrategyContinuation.DontPlayCard;
                    }
                    else
                    {
                        StrategyLogger.Debug("skipping emergency strategic draw for evil card");
                    }
                }
            }

            return StrategyContinuation.ContinueToNextStrategy;
        }

        /// <summary>
        /// Strategy: honor color requests.
        /// </summary>
        protected StrategyContinuation StrategyHonorColorRequests(List<Card> possibleCards)
        {
            if (!ColorRequest.HasValue)
            {
                // no color request to honor
                return StrategyContinuation.ContinueToNextStrategy;
            }

            if (TopCard.Color == ColorRequest.Value)
            {
                // glad that's been taken care of
                ColorRequest = null;
                return StrategyContinuation.ContinueToNextStrategy;
            }

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
                return StrategyContinuation.SkipAllOtherStrategies;
            }

            return StrategyContinuation.ContinueToNextStrategy;
        }

        /// <summary>
        /// Strategy: weight cards by type.
        /// </summary>
        protected StrategyContinuation StrategyWeightByType(List<Card> possibleCards)
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

            return StrategyContinuation.ContinueToNextStrategy;
        }

        /// <summary>
        /// Filter: if the previous player has too few cards, filter out reverses.
        /// </summary>
        protected void FilterReverseIfPreviousWinning(List<Card> possibleCards)
        {
            if (CurrentHand.Count <= Config.PlayToWinThreshold)
            {
                // not filtering anything; I'm about to win!
                return;
            }

            if (PreviousPlayer != null && CurrentCardCounts.ContainsKey(PreviousPlayer) && CurrentCardCounts[PreviousPlayer] <= Config.PlayToWinThreshold)
            {
                StrategyLogger.DebugFormat("previous player ({0}) has {1} cards or less ({2}); filtering out reverses", PreviousPlayer, Config.PlayToWinThreshold, CurrentCardCounts[PreviousPlayer]);
                possibleCards.RemoveAll(c => c.Value == CardValue.Reverse);
            }
        }

        /// <summary>
        /// Filter: if the next-but-one player has too few cards, filter out any cards that would skip right to them.
        /// </summary>
        protected void FilterAnySkipsIfNextButOneWinning(List<Card> possibleCards)
        {
            if (CurrentHand.Count <= Config.PlayToWinThreshold)
            {
                // not filtering anything; I'm about to win!
                return;
            }

            if (NextButOnePlayer != null && CurrentCardCounts.ContainsKey(NextButOnePlayer) && CurrentCardCounts[NextButOnePlayer] <= Config.PlayToWinThreshold)
            {
                StrategyLogger.DebugFormat("next-but-one player ({0}) has {1} cards or less ({2}); filtering out cards that would skip my successor (their predecessor)", NextButOnePlayer, Config.PlayToWinThreshold, CurrentCardCounts[NextButOnePlayer]);
                possibleCards.RemoveAll(c => c.Value == CardValue.DrawTwo || c.Value == CardValue.WildDrawFour || c.Value == CardValue.Skip);
            }
        }

        /// <summary>
        /// Picks the card that should be played from a list of possible cards.
        /// </summary>
        /// <returns>The card to play, or <c>null</c> to draw a card instead.</returns>
        /// <param name="possibleCards">Cards to choose from.</param>
        protected virtual Card? PerformFinalPick(List<Card> possibleCards)
        {
            if (possibleCards.Count == 0)
            {
                // no card to choose from
                return null;
            }

            // pick a card at random
            var index = Randomizer.Next(possibleCards.Count);
            var card = possibleCards[index];

            // if more than two cards in hand, perform a strategic draw every once in a while
            if (Config.StrategicDrawDenominator > 0 && CurrentHand.Count > Config.PlayToWinThreshold && !DrewLast)
            {
                var strategicDraw = (Randomizer.Next(Config.StrategicDrawDenominator) == 0);
                if (strategicDraw)
                {
                    StrategyLogger.Debug("strategic draw");
                    return null;
                }
            }

            return card;
        }

        protected virtual StrategyFunction[] AssembleStrategies()
        {
            return new StrategyFunction[]
            {
                // strategy 1: destroy the next player if they are close to winning
                StrategyDestroyNextPlayerIfDangerous,

                // strategy 2: honor color requests
                StrategyHonorColorRequests,

                // strategy 3: weight cards by type
                StrategyWeightByType
            };
        }

        protected virtual FilterFunction[] AssembleFilters()
        {
            return new FilterFunction[]
            {
                // filter 1: if the previous player has too few cards, filter out reverses
                FilterReverseIfPreviousWinning,

                // filter 2: if the next-but-one player has too few cards, filter out any card that would skip right to them
                FilterAnySkipsIfNextButOneWinning
            };
        }

        protected bool IsCardPlayable(Card card)
        {
            if (card.Color == TopCard.Color)
            {
                return true;
            }
            if (card.Value == TopCard.Value)
            {
                return true;
            }
            if (card.Color == CardColor.Wild)
            {
                return true;
            }

            return false;
        }

        protected virtual void PlayACard()
        {
            var possibleCards = new List<Card>();

            var strategies = AssembleStrategies();
            var filters = AssembleFilters();

            if (StrategyLogger.IsDebugEnabled)
            {
                StrategyLogger.DebugFormat("current hand: [{0}]", string.Join(", ", CurrentHand.Select(c => c.Color + " " + c.Value)));
            }

            if (Config.DrawAllTheTime && !DrewLast)
            {
                DrawACard();
                return;
            }

            // apply strategies
            foreach (var strategy in strategies)
            {
                var continuation = strategy(possibleCards);
                if (continuation == StrategyContinuation.ContinueToNextStrategy)
                {
                    continue;
                }
                else if (continuation == StrategyContinuation.DontPlayCard)
                {
                    return;
                }
                else if (continuation == StrategyContinuation.SkipAllOtherStrategies)
                {
                    break;
                }
            }

            // apply filters
            foreach (var filter in filters)
            {
                filter(possibleCards);
            }

            // perform the final pick
            var maybeCard = PerformFinalPick(possibleCards);
            if (maybeCard.HasValue)
            {
                // final pick chose a card
                var card = maybeCard.Value;

                StrategyLogger.DebugFormat("playing card: {0} {1}", card.Color, card.Value);

                if (card.Color == CardColor.Wild)
                {
                    // pick a color
                    var chosenColor = PickAColor();
                    StrategyLogger.DebugFormat("chosen color: {0}", chosenColor);

                    // play the card
                    PlayWildCard(card.Value, chosenColor);
                }
                else
                {
                    // play it
                    PlayColorCard(card);
                }
                DrewLast = false;
                DrawsSinceLastPlay = 0;
                return;
            }

            if (DrewLast)
            {
                StrategyLogger.Debug("passing");
                PassAfterDrawing();
            }
            else
            {
                if (Config.ManyDrawsCurseThreshold >= 0 && DrawsSinceLastPlay > Config.ManyDrawsCurseThreshold)
                {
                    StrategyLogger.Debug("cursing because of too many draws");
                    Curse();
                }
                StrategyLogger.Debug("drawing");
                DrawACard();
            }
        }
    }
}
