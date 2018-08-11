namespace SharpIrcBot.Plugins.Libraries.RegularExpressionReplacement.Internal
{
    public interface IPlaceholder
    {
        string Replace(ReplacementState state);
    }
}
