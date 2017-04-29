using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SharpIrcBot.Plugins.CasinoBot.Cards;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    public class TabularCardCounter : ICardCounter
    {
        /*     A   2   3   4   5   6   7   8   9  10   J   Q   K */
        private static readonly int[] _hiLoTable =
            { -1, +1, +1, +1, +1, +1,  0,  0,  0, -1, -1, -1, -1 };
        private static readonly int[] _hiOpt1Table =
            {  0,  0, +1, +1, +1, +1,  0,  0,  0, -1, -1, -1, -1 };
        private static readonly int[] _hiOpt2Table =
            {  0, +1, +1, +2, +2, +1, +1,  0,  0, -2, -2, -2, -2 };
        private static readonly int[] _koTable =
            { -1, +1, +1, +1, +1, +1, +1,  0,  0, -1, -1, -1, -1 };
        private static readonly int[] _omega2Table =
            {  0, +1, +1, +2, +2, +2, +1,  0, -1, -2, -2, -2, -2 };
        private static readonly int[] _red7Zero7Table =
            { -1, +1, +1, +1, +1, +1,  0,  0,  0, -1, -1, -1, -1 };
        private static readonly int[] _red7One7Table =
            { -1, +1, +1, +1, +1, +1, +1,  0,  0, -1, -1, -1, -1 };
        private static readonly int[] _halvesDoubledTable =
            { -2, +1, +2, +2, +3, +2, +1,  0, -1, -2, -2, -2, -2 };
        private static readonly int[] _zenTable =
            { -1, +1, +1, +2, +2, +2, +1,  0,  0, -2, -2, -2, -2 };

        public static readonly Lazy<TabularCardCounter> HiLo = MakeLazyTabularCardCounter(_hiLoTable);
        public static readonly Lazy<TabularCardCounter> HiOpt1 = MakeLazyTabularCardCounter(_hiOpt1Table);
        public static readonly Lazy<TabularCardCounter> HiOpt2 = MakeLazyTabularCardCounter(_hiOpt2Table);
        public static readonly Lazy<TabularCardCounter> KO = MakeLazyTabularCardCounter(_koTable);
        public static readonly Lazy<TabularCardCounter> Omega2 = MakeLazyTabularCardCounter(_omega2Table);
        public static readonly Lazy<TabularCardCounter> Red7Zero7 = MakeLazyTabularCardCounter(_red7Zero7Table);
        public static readonly Lazy<TabularCardCounter> Red7One7 = MakeLazyTabularCardCounter(_red7One7Table);
        public static readonly Lazy<TabularCardCounter> HalvesDoubled = MakeLazyTabularCardCounter(_halvesDoubledTable);
        public static readonly Lazy<TabularCardCounter> Zen = MakeLazyTabularCardCounter(_zenTable);

        public ImmutableDictionary<CardValue, int> Table { get; }
        public int MinCountPerDeck { get; }
        public int MaxCountPerDeck { get; }
        public int TotalDecks { get; set; }
        protected int RunningCount { get; set; }
        protected int CardsPlayed { get; set; }
        protected int MaxBaseBet { get; set; }

        public TabularCardCounter(IEnumerable<KeyValuePair<CardValue, int>> table)
        {
            var readyTable = table as ImmutableDictionary<CardValue, int>;
            if (readyTable != null)
            {
                Table = readyTable;
            }
            else
            {
                Table = table.ToImmutableDictionary();
            }

            var zeroInt = new[] {0};
            MinCountPerDeck = Table.Values
                .Where(v => v < 0) // pick out the card values with bad adjustments
                .Concat(zeroInt) // ensure Sum() doesn't fail
                .Sum()
                * 4; // four suits
            MaxCountPerDeck = Table.Values
                .Where(v => v > 0) // pick out the card values with good adjustments
                .Concat(zeroInt)
                .Sum()
                * 4;

            RunningCount = 0;
            CardsPlayed = 0;
        }

        /// <remarks>
        /// If the number of decks played is greater than the total number of decks, assume the dealer is lying and be
        /// extremely conservative until we see that the shoe has been shuffled.
        /// </remarks>
        protected virtual bool DealerIsLying => (CardsPlayed / 52.0m > TotalDecks);
        protected virtual decimal DecksPlayed => DealerIsLying ? 0.0m : (CardsPlayed / 52.0m);
        protected virtual decimal RelevantCountPerDeck => (RunningCount < 0) ? (-MinCountPerDeck) : MaxCountPerDeck;
        public virtual decimal Risk => (RunningCount * DecksPlayed) / (RelevantCountPerDeck * TotalDecks);

        public virtual void CardDealt(Card card)
        {
            RunningCount += Table[card.Value];
            ++CardsPlayed;
        }

        public virtual void ShoeShuffled()
        {
            RunningCount = 0;
            CardsPlayed = 0;
        }

        public override string ToString()
        {
            return $"[RC({RunningCount})*DP({DecksPlayed:F2}{(DealerIsLying?"L":"")})={RunningCount*DecksPlayed:F2}]/[RCPD({RelevantCountPerDeck})*TD({TotalDecks})={MaxCountPerDeck*TotalDecks}]={Risk:F2}";
        }

        private static Lazy<TabularCardCounter> MakeLazyTabularCardCounter(int[] tableArray)
        {
            ImmutableDictionary<CardValue, int> table = tableArray
                .Select((val, i) => new KeyValuePair<CardValue, int>((CardValue)((int)CardValue.Ace + i), val))
                .ToImmutableDictionary();
            return new Lazy<TabularCardCounter>(() => new TabularCardCounter(table));
        }
    }
}
