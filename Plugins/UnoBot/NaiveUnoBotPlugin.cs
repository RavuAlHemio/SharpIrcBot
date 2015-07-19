using System;
using System.Linq;
using SharpIrcBot;
using Newtonsoft.Json.Linq;

namespace UnoBot
{
    public class NaiveUnoBotPlugin : UnoBotPlugin
    {
        public NaiveUnoBotPlugin(ConnectionManager connMgr, JObject config)
            : base(connMgr, config)
        {
        }

        protected static int MalusForCard(Card card)
        {
            int malus = 0;

            switch (card.Color)
            {
                case CardColor.Red:
                    malus += 100;
                    break;
                case CardColor.Green:
                    malus += 200;
                    break;
                case CardColor.Blue:
                    malus += 300;
                    break;
                case CardColor.Yellow:
                    malus += 400;
                    break;
                case CardColor.Wild:
                    malus += 500;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("card", "card has unknown color");
            }

            switch (card.Value)
            {
                case CardValue.Zero:
                    malus += 12;
                    break;
                case CardValue.One:
                    malus += 11;
                    break;
                case CardValue.Two:
                    malus += 10;
                    break;
                case CardValue.Three:
                    malus += 9;
                    break;
                case CardValue.Four:
                    malus += 8;
                    break;
                case CardValue.Five:
                    malus += 7;
                    break;
                case CardValue.Six:
                    malus += 6;
                    break;
                case CardValue.Seven:
                    malus += 5;
                    break;
                case CardValue.Eight:
                    malus += 4;
                    break;
                case CardValue.Nine:
                    malus += 3;
                    break;
                case CardValue.Skip:
                    malus += 2;
                    break;
                case CardValue.Reverse:
                    malus += 1;
                    break;
                case CardValue.DrawTwo:
                    malus += 0;
                    break;
                case CardValue.Wild:
                    malus += 13;
                    break;
                case CardValue.WildDrawFour:
                    malus += 14;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("card", "card has unknown value");
            }

            return malus;
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

        protected override void PlayACard()
        {
            // find a playable card with the lowest malus
            var cardToPlay = CurrentHand
                .Where(IsCardPlayable)
                .OrderBy(MalusForCard)
                .Select(c => (Card?)c)
                .FirstOrDefault();
           
            if (cardToPlay.HasValue)
            {
                if (cardToPlay.Value.Color == CardColor.Wild)
                {
                    // choose the color of which we have the most cards, or red
                    var colorsByCount = CurrentHand
                        .Where(c => c.Color != CardColor.Wild)
                        .GroupBy(c => c.Color)
                        .Select(grp => new { Color = grp.Key, Count = grp.Count() })
                        .OrderByDescending(cc => cc.Count)
                        .ToList();
                    var colorToPlay = (colorsByCount.Count > 0) ? colorsByCount[0].Color : CardColor.Red;

                    PlayWildCard(cardToPlay.Value.Value, colorToPlay);
                }
                else
                {
                    PlayColorCard(cardToPlay.Value);
                }
            }
            else
            {
                if (DrewLast)
                {
                    PassAfterDrawing();
                }
                else
                {
                    DrawACard();
                }
            }
        }
    }
}
