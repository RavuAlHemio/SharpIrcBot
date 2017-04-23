using System;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.CasinoBot.Cards
{
    public static class CardUtils
    {
        public static char ToUnicode(this CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Hearts:
                    return '\u2665';
                case CardSuit.Diamonds:
                    return '\u2666';
                case CardSuit.Clubs:
                    return '\u2663';
                case CardSuit.Spades:
                    return '\u2660';
            }

            throw new ArgumentOutOfRangeException(nameof(suit), suit, "invalid suit");
        }

        public static char ToUnicode(this CardValue value)
        {
            switch (value)
            {
                case CardValue.Ace:
                    return 'A';
                case CardValue.Two:
                    return '2';
                case CardValue.Three:
                    return '3';
                case CardValue.Four:
                    return '4';
                case CardValue.Five:
                    return '5';
                case CardValue.Six:
                    return '6';
                case CardValue.Seven:
                    return '7';
                case CardValue.Eight:
                    return '8';
                case CardValue.Nine:
                    return '9';
                case CardValue.Ten:
                    return 'T';
                case CardValue.Jack:
                    return 'J';
                case CardValue.Queen:
                    return 'Q';
                case CardValue.King:
                    return 'K';
            }

            throw new ArgumentOutOfRangeException(nameof(value), value, "invalid value");
        }

        public static char ToWhiteUnicode(this CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Hearts:
                    return '\u2661';
                case CardSuit.Diamonds:
                    return '\u2662';
                case CardSuit.Clubs:
                    return '\u2667';
                case CardSuit.Spades:
                    return '\u2664';
            }

            throw new ArgumentOutOfRangeException(nameof(suit), suit, "invalid suit");
        }

        public static CardSuit? MaybeParseSuit(char suitChar)
        {
            switch (suitChar)
            {
                case '\u2665':
                case '\u2661':
                case 'H':
                case 'h':
                    return CardSuit.Hearts;
                case '\u2666':
                case '\u2662':
                case 'D':
                case 'd':
                    return CardSuit.Diamonds;
                case '\u2663':
                case '\u2667':
                case 'C':
                case 'c':
                    return CardSuit.Clubs;
                case '\u2660':
                case '\u2664':
                case 'S':
                case 's':
                    return CardSuit.Spades;
            }

            return null;
        }

        public static CardSuit ParseSuit(char suitChar)
        {
            CardSuit? suit = MaybeParseSuit(suitChar);
            if (!suit.HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(suitChar), suitChar, "invalid suit character");
            }
            return suit.Value;
        }

        public static CardValue? MaybeParseValue(char valueChar)
        {
            switch (valueChar)
            {
                case 'A':
                    return CardValue.Ace;
                case '2':
                    return CardValue.Two;
                case '3':
                    return CardValue.Three;
                case '4':
                    return CardValue.Four;
                case '5':
                    return CardValue.Five;
                case '6':
                    return CardValue.Six;
                case '7':
                    return CardValue.Seven;
                case '8':
                    return CardValue.Eight;
                case '9':
                    return CardValue.Nine;
                case 'T':
                    return CardValue.Ten;
                case 'J':
                    return CardValue.Jack;
                case 'Q':
                    return CardValue.Queen;
                case 'K':
                    return CardValue.King;
            }

            return null;
        }

        public static CardValue ParseValue(char valueChar)
        {
            CardValue? value = MaybeParseValue(valueChar);
            if (!value.HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(valueChar), valueChar, "invalid value character");
            }
            return value.Value;
        }

        public static Card CardFromJson(JObject cardObject)
        {
            if (cardObject.Count != 2)
            {
                throw new ArgumentException($"{nameof(cardObject)} has {cardObject.Count} name/value pairs; expected 2", nameof(cardObject));
            }

            JToken suitToken, faceToken;

            if (!cardObject.TryGetValue("suit", StringComparison.Ordinal, out suitToken))
            {
                throw new ArgumentException($"{nameof(cardObject)} does not contain a name/value pair named \"suit\"", nameof(cardObject));
            }
            if (suitToken.Type != JTokenType.String)
            {
                throw new ArgumentException($"{nameof(cardObject)} value for \"suit\" is not a string value", nameof(cardObject));
            }

            if (!cardObject.TryGetValue("face", StringComparison.Ordinal, out faceToken))
            {
                throw new ArgumentException($"{nameof(cardObject)} does not contain a name/value pair named \"face\"", nameof(cardObject));
            }
            if (faceToken.Type != JTokenType.String)
            {
                throw new ArgumentException($"{nameof(cardObject)} value for \"face\" is not a string value", nameof(cardObject));
            }

            var suitString = (string)((JValue)suitToken).Value;
            var faceString = (string)((JValue)faceToken).Value;

            if (suitString.Length != 1)
            {
                throw new ArgumentException($"{nameof(cardObject)} value for \"suit\" has {suitString.Length} characters; expected 1", nameof(cardObject));
            }
            if (faceString.Length != 1)
            {
                throw new ArgumentException($"{nameof(cardObject)} value for \"face\" has {faceString.Length} characters; expected 1", nameof(cardObject));
            }

            CardSuit? suit = MaybeParseSuit(suitString[0]);
            CardValue? value = MaybeParseValue(faceString[0]);

            if (!suit.HasValue)
            {
                throw new ArgumentException($"{nameof(cardObject)} value for \"suit\" cannot be parsed as a suit character", nameof(cardObject));
            }
            if (!value.HasValue)
            {
                throw new ArgumentException($"{nameof(cardObject)} value for \"face\" cannot be parsed as a card value character", nameof(cardObject));
            }

            return new Card(suit.Value, value.Value);
        }
    }
}
