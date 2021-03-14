using System;
using System.Collections.Immutable;
using System.Linq;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class AlternProduction : ContainerProduction
    {
        public ImmutableArray<int> InnerWeights { get; }
        public int TotalWeight { get; }

        public AlternProduction(ImmutableArray<Production> inners)
            : base(inners)
        {
            var weightsBuilder = ImmutableArray.CreateBuilder<int>(Inners.Length);
            foreach (Production prod in Inners)
            {
                var weightProd = prod as IWeightedProduction;
                weightsBuilder.Add(weightProd?.Weight ?? GrammarVisitor.DefaultWeight);
            }
            InnerWeights = weightsBuilder.MoveToImmutable();
            TotalWeight = InnerWeights.Sum();
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            if (Inners.Length == 1)
            {
                // fast path
                return Inners[0].Produce(rng, rulebook, parameters);
            }

            int weighted = rng.Next(TotalWeight);
            for (int i = 0; i < InnerWeights.Length; i++)
            {
                if (weighted < InnerWeights[i])
                {
                    // produce this one
                    return Inners[i].Produce(rng, rulebook, parameters);
                }
                else
                {
                    // perhaps the next one?
                    weighted -= InnerWeights[i];
                }
            }

            // this shouldn't happen
            throw new NotImplementedException("unreachable code reached");
        }

        public override string ToString()
        {
            string bits = string.Join(" | ", Inners.Select(i => i.ToString()));
            return $"({bits})";
        }
    }
}
