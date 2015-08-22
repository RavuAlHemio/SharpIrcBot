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

        public int Malus
        {
            get
            {
                int malus = 0;

                switch (Color)
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
                        throw new InvalidOperationException("card has unknown color");
                }

                switch (Value)
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
                        throw new InvalidOperationException("card has unknown value");
                }

                return malus;
            }
        }
    }
}
