using System;
using System.Collections;
using System.Collections.Generic;
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

        protected virtual ImmutableArray<Production> GetInnersMatchingCondition(ImmutableDictionary<string, object> parameters)
        {
            return Inners
                .Where(i =>
                    !(i is IConditionalProduction)
                    || ((IConditionalProduction)i).Conditions.All(cond =>
                        // negated conditions are stored as the identifier prefixed with a !
                        cond.StartsWith("!")
                            ? !IsValueTruthy(parameters, cond.Substring(1))
                            : IsValueTruthy(parameters, cond)
                    )
                )
                .ToImmutableArray();
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            ImmutableArray<Production> condInners = GetInnersMatchingCondition(parameters);

            if (condInners.Length == 0)
            {
                throw new NoProductionsRemainException(this);
            }

            if (condInners.Length == 1)
            {
                // fast path
                return condInners[0].Produce(rng, rulebook, parameters);
            }

            ImmutableArray<int> condInnerWeights = condInners
                .Select(i =>
                    (i is IWeightedProduction)
                        ? ((IWeightedProduction)i).Weight
                        : GrammarVisitor.DefaultWeight
                )
                .ToImmutableArray();
            int totalWeight = condInnerWeights.Sum();

            int weighted = rng.Next(totalWeight);
            for (int i = 0; i < condInnerWeights.Length; i++)
            {
                if (weighted < condInnerWeights[i])
                {
                    // produce this one
                    return condInners[i].Produce(rng, rulebook, parameters);
                }
                else
                {
                    // perhaps the next one?
                    weighted -= condInnerWeights[i];
                }
            }

            // this shouldn't happen
            throw new NotImplementedException("unreachable code reached");
        }

        public override IEnumerable<string> ProduceAll(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            ImmutableArray<Production> condInners = GetInnersMatchingCondition(parameters);

            if (condInners.Length == 0)
            {
                throw new NoProductionsRemainException(this);
            }

            foreach (Production condInner in condInners)
            {
                foreach (string produced in condInner.ProduceAll(rulebook, parameters))
                {
                    yield return produced;
                }
            }
        }

        public override string ToString()
        {
            string bits = string.Join(" | ", Inners.Select(i => i.ToString()));
            return $"({bits})";
        }

        static bool IsValueTruthy(ImmutableDictionary<string, object> dict, string key)
        {
            object val;
            if (!dict.TryGetValue(key, out val))
            {
                return false;
            }

            if (val == null)
            {
                return false;
            }

            if (val is bool)
            {
                return (bool)val;
            }

            string strVal = val as string;
            if (strVal != null)
            {
                return strVal.Length > 0;
            }

            if (val is byte)
            {
                return ((byte)val != 0);
            }
            if (val is short)
            {
                return ((short)val != 0);
            }
            if (val is int)
            {
                return ((int)val != 0);
            }
            if (val is long)
            {
                return ((long)val != 0);
            }
            if (val is sbyte)
            {
                return ((sbyte)val != 0);
            }
            if (val is ushort)
            {
                return ((ushort)val != 0);
            }
            if (val is uint)
            {
                return ((uint)val != 0);
            }
            if (val is ulong)
            {
                return ((ulong)val != 0);
            }
            if (val is float)
            {
                return ((float)val != 0.0);
            }
            if (val is double)
            {
                return ((double)val != 0.0);
            }

            ICollection collVal = val as ICollection;
            if (collVal != null)
            {
                return collVal.Count > 0;
            }

            // assume true because it's not null
            return true;
        }
    }
}
