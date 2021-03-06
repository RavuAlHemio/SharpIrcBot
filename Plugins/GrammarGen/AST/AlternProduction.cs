using System;
using System.Collections.Immutable;
using System.Linq;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class AlternProduction : ContainerProduction
    {
        public AlternProduction(ImmutableArray<Production> inners)
            : base(inners)
        {
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            int index = rng.Next(Inners.Length);
            return Inners[index].Produce(rng, rulebook, parameters);
        }

        public override string ToString()
        {
            string bits = string.Join(" | ", Inners.Select(i => i.ToString()));
            return $"({bits})";
        }
    }
}
