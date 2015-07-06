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

namespace UnoBot
{
    public class UnoBotPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected const string UnoMessagePrefix = "###   ";
        protected const string CurrentPlayerEventName = "current_player";
        protected const string TopCardEventName = "current_card";
        protected const string HandInfoEventName = "hand_info";

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
        protected int LastHandCount;
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
            LastHandCount = -1;
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
                    // my turn? not my turn?
                    var currentPlayer = (string) evt["player"];
                    MyTurn = (currentPlayer == ConnectionManager.Client.Nickname);
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

            if (possibleCards.Count > 0)
            {
                // pick one at random
                var index = Randomizer.Next(possibleCards.Count);
                var card = possibleCards[index];

                // if more than two cards in hand, perform a strategic draw 10% of the time
                if (CurrentHand.Count > 2 && !DrewLast)
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
                    Curse();
                }
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "!draw");
            }
        }
    }
}
