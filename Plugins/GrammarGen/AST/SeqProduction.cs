using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class SeqProduction : Production
    {
        public ImmutableArray<Production> Inners { get; }

        public SeqProduction(ImmutableArray<Production> inners)
        {
            Inners = inners;
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            var ret = new StringBuilder();
            foreach (Production inner in Inners)
            {
                ret.Append(inner.Produce(rng, rulebook, parameters));
            }
            return ret.ToString();
        }

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
            foreach (Production inner in Inners)
            {
                inner.CollectSoundnessErrors(rulebook, errors);
            }
        }

        public override string ToString()
        {
            string inners = string.Join(" ", Inners.Select(i => i.ToString()));
            return $"({inners})";
        }
    }
}
