namespace Allograph.RegularExpressions.Internal
{
    public class LookupPlaceholder : IPlaceholder
    {
        public string LookupKey { get; }

        public LookupPlaceholder(string lookupKey)
        {
            LookupKey = lookupKey;
        }

        public string Replace(ReplacementState state)
        {
            return state.Lookups[LookupKey];
        }
    }
}
