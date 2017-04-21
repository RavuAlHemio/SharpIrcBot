using System.Collections.Generic;

namespace SharpIrcBot.Plugins.CasinoBot.Cards
{
    public static class BlackjackUtils
    {
        public static IEnumerable<int> BlackjackValues(this Card card)
        {
            switch (card.Value)
            {
                case CardValue.Ace:
                    yield return 1;
                    yield return 11;
                    break;
                case CardValue.Jack:
                case CardValue.Queen:
                case CardValue.King:
                    yield return 10;
                    break;
                default:
                    yield return (int)card.Value;
                    break;
            }
        }

        public static IEnumerable<int> BlackjackValues(this IEnumerable<Card> cards)
        {
            var currentValues = new List<int> { 0 };

            foreach (Card card in cards)
            {
                IEnumerable<int> cardValues = card.BlackjackValues();
                var newValues = new List<int>();

                foreach (int cardValue in cardValues)
                {
                    foreach (int currentValue in currentValues)
                    {
                        newValues.Add(cardValue + currentValue);
                    }
                }

                currentValues = newValues;
            }

            return currentValues;
        }
    }
}
