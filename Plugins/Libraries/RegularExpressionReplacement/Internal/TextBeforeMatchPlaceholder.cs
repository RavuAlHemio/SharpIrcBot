namespace SharpIrcBot.Plugins.Libraries.RegularExpressionReplacement.Internal
{
    public class TextBeforeMatchPlaceholder : IPlaceholder
    {
        public TextBeforeMatchPlaceholder()
        {
        }

        public string Replace(ReplacementState state)
        {
            return state.InputString.Substring(0, state.Match.Index);
        }
    }
}
