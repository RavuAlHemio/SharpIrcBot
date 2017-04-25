namespace SharpIrcBot.Commands
{
    public interface IArgumentTaker
    {
        string Take(string input, out object value);
    }
}
