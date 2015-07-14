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
            return new Card { Color = color.Value, Value = value.Value };
        }
    }
}

