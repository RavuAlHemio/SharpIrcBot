namespace Allograph.RegularExpressions.Internal
{
    public class LastGroupCapturedPlaceholder : IPlaceholder
    {
        public LastGroupCapturedPlaceholder()
        {
        }

        public string Replace(ReplacementState state)
        {
            return state.Match.Groups[state.Match.Groups.Count-1].Value;
        }
    }
}
