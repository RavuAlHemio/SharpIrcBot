using SharpIrcBot.Plugins.CasinoBot.Cards;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    public interface ICardCounter
    {
        void CardDealt(Card card);

        void ShoeShuffled();

        void UpdateFromConfig(PlayerConfig config);

        int TotalDecks { get; set; }

        decimal BetAmount { get; }
    }
}
