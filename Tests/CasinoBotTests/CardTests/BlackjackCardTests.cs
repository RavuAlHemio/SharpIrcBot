using System;
using System.Collections.Generic;
using SharpIrcBot.Plugins.CasinoBot.Cards;
using Xunit;

namespace SharpIrcBot.Tests.CasinoBotTests.CardTests
{
    public class BlackjackCardTests
    {
        static T[] MakeArray<T>(params T[] values)
        {
            return values;
        }

        static void AssertSequenceEqual<T>(IEnumerable<T> actual, params T[] expected)
        {
            Assert.Equal<T>((IEnumerable<T>)expected, actual);
        }

        [Fact]
        public void TestEmptyHand()
        {
            Card[] emptyHand = MakeArray<Card>();
            AssertSequenceEqual(emptyHand.BlackjackValues(), 0);
        }

        [Fact]
        public void TestSimpleHand()
        {
            Card[] simpleHand = MakeArray<Card>(
                new Card(CardSuit.Hearts, CardValue.Eight),
                new Card(CardSuit.Diamonds, CardValue.Queen),
                new Card(CardSuit.Clubs, CardValue.Three),
                new Card(CardSuit.Spades, CardValue.Ten)
            );
            AssertSequenceEqual(simpleHand.BlackjackValues(), 31);
        }

        [Fact]
        public void TestSingleAceHand()
        {
            Card[] simpleHand = MakeArray<Card>(
                new Card(CardSuit.Hearts, CardValue.Ace),
                new Card(CardSuit.Diamonds, CardValue.Queen),
                new Card(CardSuit.Clubs, CardValue.Three),
                new Card(CardSuit.Spades, CardValue.Ten)
            );
            AssertSequenceEqual(simpleHand.BlackjackValues(), 24, 34);
        }

        [Fact]
        public void TestMultipleAceHand()
        {
            Card[] simpleHand = MakeArray<Card>(
                new Card(CardSuit.Hearts, CardValue.Eight),
                new Card(CardSuit.Diamonds, CardValue.Ace),
                new Card(CardSuit.Diamonds, CardValue.Five),
                new Card(CardSuit.Clubs, CardValue.Ace),
                new Card(CardSuit.Spades, CardValue.Nine)
            );
            AssertSequenceEqual(simpleHand.BlackjackValues(), 24, 34, 34, 44);
        }
    }
}
