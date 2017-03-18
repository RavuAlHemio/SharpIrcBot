using JetBrains.Annotations;

namespace SharpIrcBot.Plugins.LinkInfo
{
    public interface ILinkResolverPlugin
    {
        [CanBeNull]
        LinkAndInfo ResolveLink([NotNull] LinkToResolve link);
    }
}
