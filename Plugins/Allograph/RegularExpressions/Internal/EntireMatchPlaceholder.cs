namespace Allograph.RegularExpressions.Internal
{
    public class EntireMatchPlaceholder : IPlaceholder
    {
        public EntireMatchPlaceholder()
        {
        }

        public string Replace(ReplacementState state)
        {
            return state.Match.Value;
        }
    }
}
