using System;
using System.Collections.Generic;
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

        public override IEnumerable<string> ProduceAll(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            foreach (string innerProduced in Inner.ProduceAll(rulebook, parameters))
            {
                yield return innerProduced;
            }
            yield return "";
        }

        public override CountBounds CountVariantBounds(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            CountBounds innerBounds = Inner.CountVariantBounds(rulebook, parameters);
            return new CountBounds(innerBounds.Lower + 1, innerBounds.Upper + 1);
        }

        public override string ToString()
        {
            return $"[<{Weight}> {Inner.ToString()}]";
        }
    }
}
