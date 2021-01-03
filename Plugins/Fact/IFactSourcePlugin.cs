using System;
using JetBrains.Annotations;

namespace SharpIrcBot.Plugins.Fact
{
    public interface IFactSourcePlugin
    {
        [CanBeNull]
        string GetRandomFact([NotNull] Random rng);
    }
}
