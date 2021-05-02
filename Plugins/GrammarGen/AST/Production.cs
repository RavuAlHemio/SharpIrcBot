using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public abstract class Production : ASTNode
    {
        public abstract string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters);

        public abstract IEnumerable<string> ProduceAll(Rulebook rulebook, ImmutableDictionary<string, object> parameters);

        public abstract void CollectSoundnessErrors(Rulebook rulebook, List<string> errors);
    }
}
