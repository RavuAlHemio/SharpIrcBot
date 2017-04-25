namespace SharpIrcBot.Commands
{
    public interface IArgumentMatcher
    {
        bool Match(string input, out object value);
    }
}
