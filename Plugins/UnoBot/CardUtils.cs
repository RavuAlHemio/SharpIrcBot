using System;
using System.Globalization;

namespace UnoBot
{
    public static class CardUtils
    {
        public static CardValue? ParseValue(string str)
        {
            switch (str.ToUpperInvariant())
            {
                case "ZERO":
                case "0":
                    return CardValue.Zero;
                case "ONE":
                case "1":
                    return CardValue.One;
                case "TWO":
                case "2":
                    return CardValue.Two;
                case "THREE":
                case "3":
                    return CardValue.Three;
                case "FOUR":
                case "4":
                    return CardValue.Four;
                case "FIVE":
                case "5":
                    return CardValue.Five;
                case "SIX":
                case "6":
                    return CardValue.Six;
                case "SEVEN":
                case "7":
                    return CardValue.Seven;
                case "EIGHT":
                case "8":
                    return CardValue.Eight;
                case "NINE":
                case "9":
                    return CardValue.Nine;
                case "R":
                    return CardValue.Reverse;
                case "S":
                    return CardValue.Skip;
                case "WILD":
                case "W":
                    return CardValue.Wild;
                case "D2":
                    return CardValue.DrawTwo;
                case "WD4":
                    return CardValue.WildDrawFour;
                default:
                    return null;
            }
        }

        public static string ToShortPlayString(this CardValue value)
        {
            int numericValue = (int)value;
            if (numericValue >= 0 && numericValue <= 9)
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }
            switch (value)
            {
                case CardValue.Reverse:
                    return "r";
                case CardValue.Skip:
                    return "s";
                case CardValue.DrawTwo:
                    return "d2";
                case CardValue.Wild:
                    return "w";
                case CardValue.WildDrawFour:
                    return "wd4";
            }
            return null;
        }

        public static string ToFullPlayString(this CardValue value)
        {
            if (value >= CardValue.Zero && value <= CardValue.Nine)
            {
                return value.ToString().ToUpperInvariant();
            }
            switch (value)
            {
                case CardValue.Reverse:
                    return "R";
                case CardValue.Skip:
                    return "S";
                case CardValue.DrawTwo:
                    return "D2";
                case CardValue.Wild:
                    return "WILD";
                case CardValue.WildDrawFour:
                    return "WD4";
            }
            return null;
        }

        public static CardColor? ParseColor(string str)
        {
            switch (str.ToUpperInvariant())
            {
                case "RED":
                case "R":
                    return CardColor.Red;
                case "GREEN":
                case "G":
                    return CardColor.Green;
                case "BLUE":
                case "B":
                    return CardColor.Blue;
                case "YELLOW":
                case "Y":
                    return CardColor.Yellow;
                case "WILD":
                case "W":
                    return CardColor.Wild;
                default:
                    return null;
            }
        }

        public static string ToShortPlayString(this CardColor color)
        {
            switch (color)
            {
                case CardColor.Red:
                    return "r";
                case CardColor.Green:
                    return "g";
                case CardColor.Blue:
                    return "b";
                case CardColor.Yellow:
                    return "y";
                case CardColor.Wild:
                    return "w";
            }
            return null;
        }

        public static string ToFullPlayString(this CardColor color)
        {
            return color.ToString().ToUpperInvariant();
        }

        public static Card? ParseColorAndValue(string pair)
        {
            var bits = pair.Split(' ');
            if (bits.Length != 2)
            {
                return null;
            }
            var color = ParseColor(bits[0]);
            var value = ParseValue(bits[1]);
            if (!color.HasValue || !value.HasValue)
            {
                return null;
            }
            return new Card(color.Value, value.Value);
        }

        public static bool IsValid(this Card card)
        {
            switch (card.Color)
            {
                case CardColor.Wild:
                    switch (card.Value)
                    {
                        case CardValue.Wild:
                        case CardValue.WildDrawFour:
                            return true;
                        default:
                            // wild card of wrong or unknown value
                            return false;
                    }
                case CardColor.Red:
                case CardColor.Green:
                case CardColor.Blue:
                case CardColor.Yellow:
                    switch (card.Value)
                    {
                        case CardValue.Zero:
                        case CardValue.One:
                        case CardValue.Two:
                        case CardValue.Three:
                        case CardValue.Four:
                        case CardValue.Five:
                        case CardValue.Six:
                        case CardValue.Seven:
                        case CardValue.Eight:
                        case CardValue.Nine:
                        case CardValue.Reverse:
                        case CardValue.Skip:
                        case CardValue.DrawTwo:
                            return true;
                        default:
                            // color card of wrong or unknown value
                            return false;
                    }
                default:
                    // unknown color
                    return false;
            }
        }

        public static string ToShortPlayString(this Card card)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", card.Color.ToShortPlayString(), card.Value.ToShortPlayString());
        }

        public static string ToFullPlayString(this Card card)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", card.Color.ToFullPlayString(), card.Value.ToFullPlayString());
        }

        public static bool IsWild(this CardValue value)
        {
            return (value == CardValue.Wild || value == CardValue.WildDrawFour);
        }

        public static bool IsWild(this CardColor color)
        {
            return (color == CardColor.Wild);
        }

        public static bool IsWild(this Card card)
        {
            return (card.Color.IsWild() && card.Value.IsWild());
        }
    }
}
