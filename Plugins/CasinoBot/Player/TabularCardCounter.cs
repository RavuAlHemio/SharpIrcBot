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

            RunningCount = 0;
            CardsPlayed = 0;
        }

        public virtual void UpdateFromConfig(PlayerConfig config)
        {
            MaxBaseBet = config.MaxBaseBet;
        }

        public virtual decimal BetAmount => ((decimal)MaxBaseBet / TotalDecks + RunningCount) * CardsPlayed / 52.0m;

        public virtual void CardDealt(Card card)
        {
            RunningCount += Table[card.Value];
        }

        public virtual void ShoeShuffled()
        {
            RunningCount = 0;
            CardsPlayed = 0;
        }

        public override string ToString()
        {
            decimal baseBet = (decimal)MaxBaseBet / TotalDecks;
            decimal decksPlayed = CardsPlayed / 52.0m;
            return $"[BB({baseBet})+RC({RunningCount})]*DP({decksPlayed:F2})={BetAmount:F2}";
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
