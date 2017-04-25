using System.Linq;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.UnoBot
{
    public class NaiveUnoBotPlugin : UnoBotPlugin
    {
        public NaiveUnoBotPlugin(IConnectionManager connMgr, JObject config)
            : base(connMgr, config)
        {
        }

        protected override void PlayACard()
        {
            // find a playable card with the lowest malus
            var cardToPlay = CurrentHand
                .Where(IsCardPlayable)
                .OrderBy(c => c.Malus)
                .Select(c => (Card?)c)
                .FirstOrDefault();

            if (cardToPlay.HasValue)
            {
                if (cardToPlay.Value.Color == CardColor.Wild)
                {
                    // choose the color of which we have the most cards, or red
                    var colorsByCount = CurrentHand
                        .Where(c => c.Color != CardColor.Wild)
                        .GroupBy(c => c.Color)
                        .Select(grp => new { Color = grp.Key, Count = grp.Count() })
                        .OrderByDescending(cc => cc.Count)
                        .ToList();
                    var colorToPlay = (colorsByCount.Count > 0) ? colorsByCount[0].Color : CardColor.Red;

                    PlayWildCard(cardToPlay.Value.Value, colorToPlay);
                }
                else
                {
                    PlayColorCard(cardToPlay.Value);
                }
                DrewLast = false;
            }
            else
            {
                if (DrewLast)
                {
                    PassAfterDrawing();
                }
                else
                {
                    DrawACard();
                }
            }
        }
    }
}
