namespace Allograph.RegularExpressions.Internal
{
    public class TextAfterMatchPlaceholder : IPlaceholder
    {
        public TextAfterMatchPlaceholder()
        {
        }

        public string Replace(ReplacementState state)
        {
            return state.InputString.Substring(state.Match.Index + state.Match.Length);
        }
    }
}
