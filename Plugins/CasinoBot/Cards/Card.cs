using System;

namespace SharpIrcBot.Plugins.CasinoBot.Cards
{
    public struct Card : IComparable<Card>, IEquatable<Card>
    {
        public readonly CardSuit Suit;
        public readonly CardValue Value;

        public Card(CardSuit suit, CardValue value)
        {
            Suit = suit;
            Value = value;
        }

        public string ToUnicodeString()
        {
            return new string(new[] { Suit.ToUnicode(), Value.ToUnicode() });
        }

        public override string ToString()
        {
            return $"{Value} of {Suit}";
        }

        public override int GetHashCode()
        {
            return unchecked(
                331 * Suit.GetHashCode()
                + 617 * Value.GetHashCode()
            );
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Card)other);
        }

        public bool Equals(Card other)
        {
            return
                (this.Suit == other.Suit)
                && (this.Value == other.Value)
            ;
        }

        public int CompareTo(Card other)
        {
            int comparison;

            comparison = this.Suit.CompareTo(other.Suit);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = this.Value.CompareTo(other.Value);

            return comparison;
        }

        public static bool operator==(Card left, Card right) => left.Equals(right);
        public static bool operator!=(Card left, Card right) => !(left == right);
        public static bool operator<(Card left, Card right) => left.CompareTo(right) < 0;
        public static bool operator>(Card left, Card right) => left.CompareTo(right) > 0;
        public static bool operator<=(Card left, Card right) => left.CompareTo(right) <= 0;
        public static bool operator>=(Card left, Card right) => left.CompareTo(right) >= 0;
    }
}
