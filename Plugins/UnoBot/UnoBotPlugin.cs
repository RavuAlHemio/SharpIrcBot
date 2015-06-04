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
        private static readonly Regex YourHandNotice = new Regex(string.Format("^\\[{0} {1}(?:, {0} {1})*)\\]$", ColorsRegex, ValuesRegex));
        private static readonly Regex YouDrewNotice = new Regex(string.Format("^you drew a ({0} {1})$", ColorsRegex, ValuesRegex));

        protected ConnectionManager ConnectionManager;
        protected UnoBotConfig Config;

        protected Card TopCard;
        protected List<Card> CurrentHand;
        protected bool DrewLast;
        protected Random Randomizer;

        public UnoBotPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new UnoBotConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryNotice += HandleQueryNotice;

            CurrentCardAndPlayerMessage = new Regex(string.Format("^###   ({0}) ({1})   \\|\\|   ", ColorsRegex, ValuesRegex));

            CurrentHand = new List<Card>();
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

                if (c == 0x02)
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
            var currentCardMatch = CurrentCardAndPlayerMessage.Match(strippedBody);
            if (currentCardMatch.Success)
            {
                TopCard.Color = CardUtils.ParseColor(currentCardMatch.Groups[1].Value).Value;
                TopCard.Value = CardUtils.ParseValue(currentCardMatch.Groups[2].Value).Value;
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
            var yourHandMatch = YourHandNotice.Match(strippedBody);
            if (yourHandMatch.Success)
            {
                // "[RED FOUR, GREEN FIVE]" -> "RED FOUR, GREEN FIVE"
                var handCardsString = strippedBody.Substring(1, strippedBody.Length-2);

                // "RED FOUR, GREEN FIVE" -> ["RED FOUR", "GREEN FIVE"]
                var handCardsStrings = handCardsString.Split(new[] {", "}, StringSplitOptions.None);

                // ["RED FOUR", "GREEN FIVE"] -> [Card(Red Four), Card(Green Five)]
                CurrentHand = handCardsStrings.Select(c => CardUtils.ParseColorAndValue(c).Value).ToList();

                PlayACard();
                return;
            }

            var youDrewMatch = YouDrewNotice.Match(strippedBody);
            if (youDrewMatch.Success)
            {
                var card = CardUtils.ParseColorAndValue(youDrewMatch.Groups[1].Value).Value;
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

                if (card.Color == CardColor.Wild)
                {
                    // pick a color
                    var chosenColor = (CardColor)Randomizer.Next(4);

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
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "!pass");
            }
            else
            {
                DrewLast = true;
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "!draw");
            }
        }
    }
}
