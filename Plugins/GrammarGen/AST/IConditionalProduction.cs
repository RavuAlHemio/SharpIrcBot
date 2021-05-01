using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public interface IConditionalProduction
    {
        ImmutableArray<string> Conditions { get; }
    }
}
