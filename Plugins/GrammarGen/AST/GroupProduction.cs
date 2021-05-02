using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class GroupProduction : WrapperProduction
    {
        public GroupProduction(Production inner)
            : base(inner)
        {
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            return Inner.Produce(rng, rulebook, parameters);
        }

        public override IEnumerable<string> ProduceAll(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            return Inner.ProduceAll(rulebook, parameters);
        }

        public override string ToString()
        {
            return $"({Inner})";
        }
    }
}
