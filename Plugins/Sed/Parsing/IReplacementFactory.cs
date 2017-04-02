namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public interface IReplacementFactory
    {
        ITransformCommand Construct(GenericReplacementCommand builder);
    }
}
