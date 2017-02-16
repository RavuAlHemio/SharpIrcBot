namespace Allograph.RegularExpressions.Internal
{
    public interface IPlaceholder
    {
        string Replace(ReplacementState state);
    }
}
