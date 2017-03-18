namespace SharpIrcBot.Plugins.UnoBot
{
    public enum StrategyContinuation
    {
        /// <summary>
        /// Apply filters and continue with the next strategy. If there is no next strategy, perform the final pick.
        /// </summary>
        ContinueToNextStrategy = 0,

        /// <summary>
        /// Apply filters and then skip directly to the final pick.
        /// </summary>
        SkipAllOtherStrategies = 1,

        /// <summary>
        /// Assume a different course of action was taken. Don't apply any filters or further strategies, don't perform
        /// a final pick.
        /// </summary>
        DontPlayCard = 2,

        /// <summary>
        /// Apply filters. If the filters leave at least one card in the list of possible cards, skip directly to the
        /// final pick. Otherwise, continue with the next strategy; if there is no next strategy, perform the final
        /// pick (which will necessarily lead to drawing a card).
        /// </summary>
        SkipAllOtherStrategiesUnlessFilteredEmpty = 3
    }
}
