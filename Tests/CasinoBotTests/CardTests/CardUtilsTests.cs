using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.CasinoBot.Cards;
using Xunit;

namespace SharpIrcBot.Tests.CasinoBotTests.CardTests
{
    public class CardUtilsTests
    {
        [Theory]
        [InlineData(CardValue.Ace, 'A')]
        [InlineData(CardValue.Two, '2')]
        [InlineData(CardValue.Three, '3')]
        [InlineData(CardValue.Four, '4')]
        [InlineData(CardValue.Five, '5')]
        [InlineData(CardValue.Six, '6')]
        [InlineData(CardValue.Seven, '7')]
        [InlineData(CardValue.Eight, '8')]
        [InlineData(CardValue.Nine, '9')]
        [InlineData(CardValue.Ten, 'T')]
        [InlineData(CardValue.Jack, 'J')]
        [InlineData(CardValue.Queen, 'Q')]
        [InlineData(CardValue.King, 'K')]
        public void TestValueToUnicode(CardValue value, char unicode)
        {
            Assert.Equal(unicode, value.ToUnicode());
        }

        [Theory]
        [InlineData(CardSuit.Hearts, '♥')]
        [InlineData(CardSuit.Diamonds, '♦')]
        [InlineData(CardSuit.Clubs, '♣')]
        [InlineData(CardSuit.Spades, '♠')]
        public void TestSuitToUnicode(CardSuit suit, char unicode)
        {
            Assert.Equal(unicode, suit.ToUnicode());
        }

        [Theory]
        [InlineData(CardSuit.Hearts, '♡')]
        [InlineData(CardSuit.Diamonds, '♢')]
        [InlineData(CardSuit.Clubs, '♧')]
        [InlineData(CardSuit.Spades, '♤')]
        public void TestSuitToWhiteUnicode(CardSuit suit, char unicode)
        {
            Assert.Equal(unicode, suit.ToWhiteUnicode());
        }

        [Theory]
        [InlineData(CardSuit.Clubs, CardValue.Three, "{\"face\":\"3\",\"suit\":\"♣\"}")]
        [InlineData(CardSuit.Hearts, CardValue.Four, "{\"face\":\"4\",\"suit\":\"♥\"}")]
        [InlineData(CardSuit.Hearts, CardValue.Seven, "{\"face\":\"7\",\"suit\":\"♥\"}")]
        [InlineData(CardSuit.Diamonds, CardValue.Eight, "{\"face\":\"8\",\"suit\":\"♦\"}")]
        [InlineData(CardSuit.Clubs, CardValue.Three, "{\"suit\":\"♣\",\"face\":\"3\"}")]
        [InlineData(CardSuit.Hearts, CardValue.Four, "{\"suit\":\"♥\",\"face\":\"4\"}")]
        [InlineData(CardSuit.Hearts, CardValue.Seven, "{\"suit\":\"♥\",\"face\":\"7\"}")]
        [InlineData(CardSuit.Diamonds, CardValue.Eight, "{\"suit\":\"♦\",\"face\":\"8\"}")]
        public void TestCardFromJsonSuccess(CardSuit suit, CardValue value, string cardJsonString)
        {
            JObject cardJson = JObject.Parse(cardJsonString);
            Card card = CardUtils.CardFromJson(cardJson);
            Assert.Equal(suit, card.Suit);
            Assert.Equal(value, card.Value);
        }

        [Theory]
        [InlineData("{}", "empty object")]
        [InlineData("{\"suit\":\"♣\"}", "missing face attribute")]
        [InlineData("{\"face\":\"3\"}", "missing suit attribute")]
        [InlineData("{\"suit\":\"♣\",\"face\":\"3\",\"value\":3}", "extraneous attribute")]
        [InlineData("{\"sUit\":\"♣\",\"fAce\":\"3\"}", "wrong capitalization")]
        [InlineData("{\"suit\":\"⚜\",\"face\":\"3\"}", "invalid suit")]
        [InlineData("{\"suit\":\"♣\",\"face\":\"1\"}", "invalid face")]
        public void TestCardFromJsonFailure(string cardJsonString, string comment)
        {
            JObject cardJson = JObject.Parse(cardJsonString);
            Assert.Throws<ArgumentException>("cardObject", () => CardUtils.CardFromJson(cardJson));
        }
    }
}
