using JetBrains.Annotations;

namespace LinkInfo
{
    public interface ILinkResolverPlugin
    {
        [CanBeNull]
        LinkAndInfo ResolveLink([NotNull] LinkToResolve link);
    }
}
