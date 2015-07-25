using System;

namespace UnoBot
{
    public struct Card : IComparable<Card>, IEquatable<Card>
    {
        public readonly CardColor Color;
        public readonly CardValue Value;

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

        public override bool Equals(object obj)
        {
            return obj is Card && Equals((Card)obj);
        }

        public bool Equals(Card other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return Color.GetHashCode() + 5 * Value.GetHashCode();
        }

        public static bool operator ==(Card x, Card y)
        {
            return x.Color == y.Color && x.Value == y.Value;
        }

        public static bool operator !=(Card x, Card y)
        {
            return !(x == y);
        }
    }
}
