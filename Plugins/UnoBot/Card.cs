using System;

namespace UnoBot
{
    public struct Card : IComparable<Card>
    {
        public CardColor Color;
        public CardValue Value;

        public Card(CardColor color, CardValue value)
        {
            Color = color;
            Value = value;
        }

        public int CompareTo(Card other)
        {
            int currentCompare = Color.CompareTo(other.Color);
            if (currentCompare != 0)
            {
                return currentCompare;
            }

            return Value.CompareTo(other.Value);
        }
    }
}

