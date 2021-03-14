using System;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class OptProduction : WrapperProduction, IWeightedProduction
    {
        public int Weight { get; }

        public OptProduction(Production inner, int weight)
            : base(inner)
        {
            Weight = weight;
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            int proceed = rng.Next(100);
            return (proceed < Weight)
                ? Inner.Produce(rng, rulebook, parameters)
                : "";
        }

        public override string ToString()
        {
            return $"[<{Weight}> {Inner.ToString()}]";
        }
    }
}
