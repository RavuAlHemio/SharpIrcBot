using System;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class OptProduction : WrapperProduction
    {
        public OptProduction(Production inner)
            : base(inner)
        {
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            int proceed = rng.Next(2);
            return (proceed % 2 == 0)
                ? Inner.Produce(rng, rulebook, parameters)
                : "";
        }

        public override string ToString()
        {
            return $"[{Inner.ToString()}]";
        }
    }
}
