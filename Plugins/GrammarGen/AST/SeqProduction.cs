using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class SeqProduction : Production, IConditionalProduction, IWeightedProduction
    {
        public ImmutableArray<Production> Inners { get; }
        public int Weight { get; }

        public ImmutableArray<string> Conditions { get; }

        public SeqProduction(ImmutableArray<Production> inners, int weight, ImmutableArray<string> conditions)
        {
            Inners = inners;
            Weight = weight;
            Conditions = conditions;
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            // weighting and condition decisions are made one level above (AlternProduction)

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
            string conds = string.Concat(Conditions.Select(c => $"!{c} "));
            string inners = string.Join(" ", Inners.Select(i => i.ToString()));
            return $"{conds}<{Weight}> {inners}";
        }
    }
}
