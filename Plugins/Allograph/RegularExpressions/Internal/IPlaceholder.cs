namespace SharpIrcBot.Plugins.Allograph.RegularExpressions.Internal
{
    public interface IPlaceholder
    {
        string Replace(ReplacementState state);
    }
}
