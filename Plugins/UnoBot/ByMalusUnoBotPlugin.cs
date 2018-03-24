using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.UnoBot
{
    public class ByMalusUnoBotPlugin : UnoBotPlugin
    {
        private static readonly ILogger StrategyLogger = LogUtil.LoggerFactory.CreateLogger(typeof(ByMalusUnoBotPlugin).FullName + ".Strategy");

        public ByMalusUnoBotPlugin(IConnectionManager connMgr, JObject config)
            : base(connMgr, config)
        {
        }

        protected StrategyContinuation StrategyAnyCard(List<Card> possibleCards)
        {
            // just anything
            possibleCards.AddRange(CurrentHand.Where(IsCardPlayable));
            return StrategyContinuation.ContinueToNextStrategy;
        }

        protected override StrategyFunction[] AssembleStrategies()
        {
            // don't weight cards by type
            return new StrategyFunction[]
            {
                StrategyDestroyNextPlayerIfDangerous,
                StrategyHonorColorRequests,
                StrategyAnyCard
            };
        }

        protected override Card? PerformFinalPick(List<Card> possibleCards)
        {
            var cardToPlay = possibleCards
                .Where(IsCardPlayable)
                .OrderBy(c => c.Malus)
                .Select(c => (Card?)c)
                .FirstOrDefault();

            return cardToPlay;
        }

        protected override CardColor PickAColor()
        {
            if (ColorRequest.HasValue)
            {
                // we have a pending color request; honor it
                var color = ColorRequest.Value;
                StrategyLogger.LogDebug("honoring color request {Color}", color);
                ColorRequest = null;
                return color;
            }

            // choose the color of which we have the most cards, or red
            var colorsByCount = CurrentHand
                .Where(c => c.Color != CardColor.Wild)
                .GroupBy(c => c.Color)
                .Select(grp => new { Color = grp.Key, Count = grp.Count() })
                .OrderByDescending(cc => cc.Count)
                .ToList();

            return (colorsByCount.Count > 0) ? colorsByCount[0].Color : CardColor.Red;
        }
    }
}
