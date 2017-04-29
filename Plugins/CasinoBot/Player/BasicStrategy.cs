using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using SharpIrcBot.Plugins.CasinoBot.Cards;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    public static class BasicStrategy
    {
        enum StrategyOutcome : byte
        {
            Stand = (byte)'S',
            Hit = (byte)'h',
            DoubleOrHit = (byte)'d',
            DoubleOrStand = (byte)'D',
            SplitOrStand = (byte)'P',
            SplitOrHit = (byte)'p',
            SurrenderOrHit = (byte)'G'
        }

        public const int BlackjackSafeMaximum = 11;
        public const int BlackjackDealerMinimum = 17;
        public const int BlackjackTargetValue = 21;

        // https://en.wikipedia.org/wiki/Blackjack#Basic_strategy
        // T is 10
        private static readonly string[] _hardTotalsArray =
        {
            /*        A23456789T */
            /*  5 */ "hhhhhhhhhh",
            /*  6 */ "hhhhhhhhhh",
            /*  7 */ "hhhhhhhhhh",
            /*  8 */ "hhhhhhhhhh",
            /*  9 */ "hhddddhhhh",
            /* 10 */ "hddddddddh",
            /* 11 */ "hddddddddd",
            /* 12 */ "hhhSSShhhh",
            /* 13 */ "hSSSSShhhh",
            /* 14 */ "hSSSSShhhh",
            /* 15 */ "hSSSSShhhG",
            /* 16 */ "GSSSSShhGG",
            /* 17 */ "SSSSSSSSSS",
            /* 18 */ "SSSSSSSSSS",
            /* 19 */ "SSSSSSSSSS",
            /* 20 */ "SSSSSSSSSS",
        };

        private static readonly string[] _softTotalsArray =
        {
            /*         A23456789T */
            /* A,2 */ "hhhhddhhhh",
            /* A,3 */ "hhhhddhhhh",
            /* A,4 */ "hhhdddhhhh",
            /* A,5 */ "hhhdddhhhh",
            /* A,6 */ "hhddddhhhh",
            /* A,7 */ "hSDDDDSShh",
            /* A,8 */ "SSSSSSSSSS",
            /* A,9 */ "SSSSSSSSSS",
        };

        private static readonly string[] _pairsArray =
        {
            /*              A23456789T */
            /* A,A = 13 */ "pPPPPPpppp",
            /* 2,2 =  4 */ "hpppppphhh",
            /* 3,3 =  6 */ "hpppppphhh",
            /* 4,4 =  8 */ "hhhhpphhhh",
            /* 5,5 = 10 */ "hddddddddh",
            /* 6,6 = 12 */ "hppPPPhhhh",
            /* 7,7 = 14 */ "hPPPPPphhh",
            /* 8,8 = 16 */ "pPPPPPpppp",
            /* 9,9 = 18 */ "SPPPPPSPPS",
            /* T,T = 20 */ "SSSSSSSSSS",
        };

        // hand total -> dealer card -> strategy outcome
        static readonly ImmutableDictionary<int, ImmutableDictionary<CardValue, StrategyOutcome>> HardTotals;

        // other card if one is an ace -> dealer card -> strategy outcome
        static readonly ImmutableDictionary<CardValue, ImmutableDictionary<CardValue, StrategyOutcome>> SoftTotals;

        // value of one card in a pair -> dealer card -> strategy outcome
        static readonly ImmutableDictionary<CardValue, ImmutableDictionary<CardValue, StrategyOutcome>> Pairs;

        static BasicStrategy()
        {
            // assemble HardTotals
            {
                var hardTotalsBuilder =
                    ImmutableDictionary.CreateBuilder<int, ImmutableDictionary<CardValue, StrategyOutcome>>();
                for (int iHandSum = 0; iHandSum < _hardTotalsArray.Length; ++iHandSum)
                {
                    int handSum = iHandSum + 5;

                    var thisLineBuilder = ImmutableDictionary.CreateBuilder<CardValue, StrategyOutcome>();
                    for (int iDealerCard = 0; iDealerCard < _hardTotalsArray[iHandSum].Length; ++iDealerCard)
                    {
                        // char -> byte -> StrategyOutcome
                        var outcome = (StrategyOutcome)_hardTotalsArray[iHandSum][iDealerCard];
                        Debug.Assert(Enum.IsDefined(typeof(StrategyOutcome), outcome));

                        var dealerCard = (CardValue)((int)CardValue.Ace + iDealerCard);

                        thisLineBuilder[dealerCard] = outcome;
                    }

                    hardTotalsBuilder[handSum] = thisLineBuilder.ToImmutable();
                }
                HardTotals = hardTotalsBuilder.ToImmutable();
            }

            // assemble SoftTotals
            {
                var softTotalsBuilder =
                    ImmutableDictionary.CreateBuilder<CardValue, ImmutableDictionary<CardValue, StrategyOutcome>>();
                for (int iNonAceCard = 0; iNonAceCard < _softTotalsArray.Length; ++iNonAceCard)
                {
                    var nonAceCard = (CardValue)((int)CardValue.Two + iNonAceCard);

                    var thisLineBuilder = ImmutableDictionary.CreateBuilder<CardValue, StrategyOutcome>();
                    for (int iDealerCard = 0; iDealerCard < _softTotalsArray[iNonAceCard].Length; ++iDealerCard)
                    {
                        var outcome = (StrategyOutcome)_softTotalsArray[iNonAceCard][iDealerCard];
                        Debug.Assert(Enum.IsDefined(typeof(StrategyOutcome), outcome));

                        var dealerCard = (CardValue)((int)CardValue.Ace + iDealerCard);

                        thisLineBuilder[dealerCard] = outcome;
                    }

                    softTotalsBuilder[nonAceCard] = thisLineBuilder.ToImmutable();
                }
                SoftTotals = softTotalsBuilder.ToImmutable();
            }

            // assemble Pairs
            {
                var pairsBuilder =
                    ImmutableDictionary.CreateBuilder<CardValue, ImmutableDictionary<CardValue, StrategyOutcome>>();
                for (int iPairCard = 0; iPairCard < _pairsArray.Length; ++iPairCard)
                {
                    var pairOneCardValue = (CardValue)((int)CardValue.Ace + iPairCard);

                    var thisLineBuilder = ImmutableDictionary.CreateBuilder<CardValue, StrategyOutcome>();
                    for (int iDealerCard = 0; iDealerCard < _pairsArray[iPairCard].Length; ++iDealerCard)
                    {
                        var outcome = (StrategyOutcome)_pairsArray[iPairCard][iDealerCard];
                        Debug.Assert(Enum.IsDefined(typeof(StrategyOutcome), outcome));

                        var dealerCard = (CardValue)((int)CardValue.Ace + iDealerCard);

                        thisLineBuilder[dealerCard] = outcome;
                    }

                    pairsBuilder[pairOneCardValue] = thisLineBuilder.ToImmutable();
                }
                Pairs = pairsBuilder.ToImmutable();
            }
        }

        public static CourseOfAction? ApplyStrategy(BlackjackState gameState, int handIndex)
        {
            Debug.Assert(handIndex >= 0 && handIndex < gameState.MyHands.Count);
            Hand hand = gameState.MyHands[handIndex];
            Debug.Assert(hand != null);

            CardValue dealersUpcardValue = gameState.DealersUpcard.Value;
            if (dealersUpcardValue > CardValue.Ten)
            {
                dealersUpcardValue = CardValue.Ten;
            }

            StrategyOutcome outcome;

            if (hand.Cards.BlackjackValues().Contains(BlackjackTargetValue))
            {
                // blackjack!
                outcome = StrategyOutcome.Stand;
            }
            else if (hand.Cards.Count == 2 && hand.Cards.All(c => c.Value == hand.Cards.First().Value))
            {
                // pair

                CardValue singleCardValue = hand.Cards.First().Value;
                if (singleCardValue > CardValue.Ten)
                {
                    singleCardValue = CardValue.Ten;
                }
                outcome = Pairs[singleCardValue][dealersUpcardValue];
            }
            else if (hand.Cards.Count == 2 && hand.Cards.Any(c => c.Value == CardValue.Ace))
            {
                // one is an ace

                // the other mustn't be an ace; that's handled above
                Debug.Assert(hand.Cards.Any(c => c.Value != CardValue.Ace));

                CardValue nonAceValue = hand.Cards.First(c => c.Value != CardValue.Ace).Value;
                if (nonAceValue > CardValue.Ten)
                {
                    nonAceValue = CardValue.Ten;
                }
                outcome = SoftTotals[nonAceValue][dealersUpcardValue];
            }
            else
            {
                List<int> nonBustValues = hand.Cards.BlackjackValues()
                    .Where(v => v <= BlackjackTargetValue)
                    .ToList();
                if (nonBustValues.Count == 0)
                {
                    return null;
                }

                int maxHandValueNotBust = nonBustValues.Max();
                Debug.Assert(maxHandValueNotBust >= 5 && maxHandValueNotBust <= 20);
                outcome = HardTotals[maxHandValueNotBust][dealersUpcardValue];
            }

            switch (outcome)
            {
                case StrategyOutcome.Stand:
                    return CourseOfAction.Stand;
                case StrategyOutcome.Hit:
                    return CourseOfAction.Hit;
                case StrategyOutcome.DoubleOrHit:
                    return gameState.CanDoubleDownOnHand(handIndex)
                        ? CourseOfAction.DoubleDown
                        : CourseOfAction.Hit;
                case StrategyOutcome.DoubleOrStand:
                    return gameState.CanDoubleDownOnHand(handIndex)
                        ? CourseOfAction.DoubleDown
                        : CourseOfAction.Stand;
                case StrategyOutcome.SplitOrHit:
                    return gameState.CanSplitHand(handIndex)
                        ? CourseOfAction.Split
                        : CourseOfAction.Hit;
                case StrategyOutcome.SplitOrStand:
                    return gameState.CanSplitHand(handIndex)
                        ? CourseOfAction.Split
                        : CourseOfAction.Stand;
                case StrategyOutcome.SurrenderOrHit:
                    return gameState.CanSurrender
                        ? CourseOfAction.Surrender
                        : CourseOfAction.Hit;
                default:
                    Debug.Fail("unhandled strategy outcome");
                    return null;
            }
        }
    }
}
