namespace SharpIrcBot.Plugins.Sed
{
    public interface ITransformCommand
    {
        string Transform(string text);
    }
}
