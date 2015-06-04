using System;
using System.Globalization;

namespace UnoBot
{
    public static class CardUtils
    {
        public static CardValue? ParseValue(string str)
        {
            switch (str)
            {
                case "ZERO":
                    return CardValue.Zero;
                case "ONE":
                    return CardValue.One;
                case "TWO":
                    return CardValue.Two;
                case "THREE":
                    return CardValue.Three;
                case "FOUR":
                    return CardValue.Four;
                case "FIVE":
                    return CardValue.Five;
                case "SIX":
                    return CardValue.Six;
                case "SEVEN":
                    return CardValue.Seven;
                case "EIGHT":
                    return CardValue.Eight;
                case "NINE":
                    return CardValue.Nine;
                case "R":
                    return CardValue.Reverse;
                case "S":
                    return CardValue.Skip;
                case "WILD":
                    return CardValue.Wild;
                case "D2":
                    return CardValue.DrawTwo;
                case "WD4":
                    return CardValue.WildDrawFour;
                default:
                    return null;
            }
        }

        public static string ToPlayString(this CardValue value)
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

        public static CardColor? ParseColor(string str)
        {
            switch (str)
            {
                case "RED":
                    return CardColor.Red;
                case "GREEN":
                    return CardColor.Green;
                case "BLUE":
                    return CardColor.Blue;
                case "YELLOW":
                    return CardColor.Yellow;
                case "WILD":
                    return CardColor.Wild;
                default:
                    return null;
            }
        }

        public static string ToPlayString(this CardColor color)
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

