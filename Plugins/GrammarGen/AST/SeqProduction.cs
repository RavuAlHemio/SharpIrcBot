using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
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

        public override IEnumerable<string> ProduceAll(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            // this is where the combinatorial explosion happens

            if (Inners.Length == 0)
            {
                yield return "";
                yield break;
            }

            List<IEnumerator<string>> enumerators = Inners
                .Select(i => i.ProduceAll(rulebook, parameters).GetEnumerator())
                .ToList();

            // advance all enumerators to their first position
            foreach (IEnumerator<string> enumerator in enumerators)
            {
                if (!enumerator.MoveNext())
                {
                    // one of these enumerators is empty => break out
                    foreach (IEnumerator<string> enumeratorToDispose in enumerators)
                    {
                        enumeratorToDispose.Dispose();
                    }
                    yield break;
                }
            }

            bool allRanOut = false;
            while (!allRanOut)
            {
                // yield the current state of the enumerators
                var builder = new StringBuilder();
                foreach (IEnumerator<string> enumerator in enumerators)
                {
                    builder.Append(enumerator.Current);
                }
                yield return builder.ToString();

                // advance the last enumerator
                allRanOut = true;
                for (int i = enumerators.Count - 1; i >= 0; i--)
                {
                    if (enumerators[i].MoveNext())
                    {
                        // success; no need to advance any more
                        allRanOut = false;
                        break;
                    }

                    // this enumerator has run out; restart it
                    enumerators[i].Dispose();
                    enumerators[i] = Inners[i].ProduceAll(rulebook, parameters).GetEnumerator();
                    if (!enumerators[i].MoveNext())
                    {
                        // for some reason, the enumerator no longer has values this time
                        // break out
                        allRanOut = true;
                        break;
                    }

                    // advance the one before it instead
                }
            }

            // the top enumerator has run out; we are done
            foreach (IEnumerator<string> enumerator in enumerators)
            {
                enumerator.Dispose();
            }
        }

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
            foreach (Production inner in Inners)
            {
                inner.CollectSoundnessErrors(rulebook, errors);
            }
        }

        public override CountBounds CountVariantBounds(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            // product of the bounds of all items in the sequence

            var bounds = new CountBounds(1, 1);
            foreach (Production inner in Inners)
            {
                var innerBounds = inner.CountVariantBounds(rulebook, parameters);
                bounds = new CountBounds(
                    bounds.Lower * innerBounds.Lower,
                    bounds.Upper * innerBounds.Upper
                );
            }
            return bounds;
        }

        public override string ToString()
        {
            string conds = string.Concat(Conditions.Select(c => $"!{c} "));
            string inners = string.Join(" ", Inners.Select(i => i.ToString()));
            return $"{conds}<{Weight}> {inners}";
        }
    }
}
