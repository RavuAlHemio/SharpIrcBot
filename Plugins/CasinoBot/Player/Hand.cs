using SharpIrcBot.Collections;
using SharpIrcBot.Plugins.CasinoBot.Cards;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    public class Hand
    {
        public SortedMultiset<Card> Cards { get; set; }
        public int Round { get; set; }
    }
}
