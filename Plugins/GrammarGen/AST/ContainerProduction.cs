using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public abstract class ContainerProduction : Production
    {
        public ImmutableArray<Production> Inners { get; }

        public ContainerProduction(ImmutableArray<Production> inners)
        {
            Inners = inners;
        }

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
            foreach (Production inner in Inners)
            {
                inner.CollectSoundnessErrors(rulebook, errors);
            }
        }
    }
}
