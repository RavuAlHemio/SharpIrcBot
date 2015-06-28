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
        private readonly Regex CurrentCardAndPlayerMessage;
        private const string ColorsRegex = "(?:RED|GREEN|BLUE|YELLOW|WILD)";
        private const string ValuesRegex = "(?:ZERO|ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN|EIGHT|NINE|S|R|D2|WD4|WILD)";
        // open-ended in case the hand spans multiple lines
        private static readonly Regex YourHandNotice = new Regex(string.Format("^\\[{0} {1}(?:, {0} {1})*\\]$", ColorsRegex, ValuesRegex));
        private static readonly Regex YouDrewNotice = new Regex(string.Format("^you drew a ({0} {1})$", ColorsRegex, ValuesRegex));

        protected ConnectionManager ConnectionManager;
        protected UnoBotConfig Config;

        protected Card TopCard;
        protected List<Card> CurrentHand;
        protected bool DrewLast;
        protected Random Randomizer;
        protected StringBuilder HandBuilder;

        public UnoBotPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new UnoBotConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryNotice += HandleQueryNotice;

            CurrentCardAndPlayerMessage = new Regex(string.Format("^###   ({0}) ({1})   \\|\\|   ", ColorsRegex, ValuesRegex));

            CurrentHand = new List<Card>();
            Randomizer = new Random();
            HandBuilder = null;
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

        private void HandleQueryNotice(object sender, IrcEventArgs args)
        {
            try
            {
                ActuallyHandleQueryNotice(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling notice", exc);
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
                ConnectionManager.SendChannelMessage(message.Channel, "!join");
                return;
            }

            if (message.Message == "??leave")
            {
                ConnectionManager.SendChannelMessage(message.Channel, "!leave");
                return;
            }

            var strippedBody = StripColors(message.Message);
            Logger.DebugFormat("stripped body: {0}", strippedBody);
            var currentCardMatch = CurrentCardAndPlayerMessage.Match(strippedBody);
            if (currentCardMatch.Success)
            {
                TopCard.Color = CardUtils.ParseColor(currentCardMatch.Groups[1].Value).Value;
                TopCard.Value = CardUtils.ParseValue(currentCardMatch.Groups[2].Value).Value;
                Logger.DebugFormat("current card: {0} {1}", TopCard.Color, TopCard.Value);
            }
        }

        protected void ActuallyHandleQueryNotice(object sender, IrcEventArgs args)
        {
            var message = args.Data;
            if (message.Nick == ConnectionManager.Client.Nickname)
            {
                return;
            }

            var strippedBody = StripColors(message.Message);
            Logger.DebugFormat("stripped notice: {0}", strippedBody);
            Logger.DebugFormat("stripped notice codepoints: {0}", string.Join(" ", strippedBody.Select(c => ((int)c).ToString("X"))));

            Match yourHandMatch;
            if (HandBuilder != null)
            {
                // append
                HandBuilder.Append(message.Message);

                // try
                var strippedHand = StripColors(HandBuilder.ToString());

                yourHandMatch = YourHandNotice.Match(strippedHand);
            }
            else
            {
                yourHandMatch = YourHandNotice.Match(strippedBody);
            }

            if (yourHandMatch.Success)
            {
                Logger.Debug("\"your hand\" matched");
                HandBuilder = null;

                // "[RED FOUR, GREEN FIVE]" -> "RED FOUR, GREEN FIVE"
                var handCardsString = yourHandMatch.Value.Substring(1, yourHandMatch.Value.Length - 2);

                // "RED FOUR, GREEN FIVE" -> ["RED FOUR", "GREEN FIVE"]
                var handCardsStrings = handCardsString.Split(new[] { ", " }, StringSplitOptions.None);

                // ["RED FOUR", "GREEN FIVE"] -> [Card(Red Four), Card(Green Five)]
                CurrentHand = handCardsStrings.Select(c => CardUtils.ParseColorAndValue(c).Value).ToList();

                if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("hand cards: {0}", string.Join(", ", CurrentHand.Select(c => string.Format("{0} {1}", c.Color, c.Value))));
                }

                PlayACard();
                return;
            }
            else if (HandBuilder == null && strippedBody.StartsWith("["))
            {
                // prepare this...
                HandBuilder = new StringBuilder(message.Message);
                return;
            }

            var youDrewMatch = YouDrewNotice.Match(strippedBody);
            if (youDrewMatch.Success)
            {
                Logger.Debug("\"you drew\" matched");

                var card = CardUtils.ParseColorAndValue(youDrewMatch.Groups[1].Value).Value;
                Logger.DebugFormat("additionally drawn card: {0} {1}", card.Color, card.Value);
                CurrentHand.Add(card);

                PlayACard();
                return;
            }
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
                    var colorsToChoose = new List<CardColor>();

                    // -> add all four colors once to allow for some chaotic color switching
                    colorsToChoose.Add(CardColor.Red);
                    colorsToChoose.Add(CardColor.Green);
                    colorsToChoose.Add(CardColor.Blue);
                    colorsToChoose.Add(CardColor.Yellow);

                    // -> add all the (non-wild) colors from our hand to increase the chances of a useful pick
                    colorsToChoose.AddRange(CurrentHand.Select(c => c.Color).Where(c => c != CardColor.Wild));

                    // -> choose at random
                    var chosenColor = colorsToChoose[Randomizer.Next(colorsToChoose.Count)];
                    Logger.DebugFormat("chosen color: {0}", chosenColor);

                    // play the card
                    ConnectionManager.SendChannelMessage(Config.UnoChannel,
                        string.Format("!p {0} {1}", card.Value.ToPlayString(), chosenColor.ToPlayString())
                    );
                }
                else
                {
                    // play it
                    ConnectionManager.SendChannelMessage(Config.UnoChannel,
                        string.Format("!p {0} {1}", card.Color.ToPlayString(), card.Value.ToPlayString())
                    );
                }
                DrewLast = false;
                return;
            }

            // nope
            if (DrewLast)
            {
                DrewLast = false;
                Logger.Debug("passing");
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "!pass");
            }
            else
            {
                DrewLast = true;
                Logger.Debug("drawing");
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "!draw");
            }
        }
    }
}
