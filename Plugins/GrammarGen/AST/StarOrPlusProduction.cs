using System;
using System.Collections.Immutable;
using System.Text;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class StarOrPlusProduction : WrapperProduction
    {
        public bool ZeroAllowed { get; }

        public StarOrPlusProduction(bool zeroAllowed, Production inner)
            : base(inner)
        {
            ZeroAllowed = zeroAllowed;
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            var ret = new StringBuilder();
            if (!ZeroAllowed)
            {
                ret.Append(Inner.Produce(rng, rulebook, parameters));
            }

            while (rng.Next(2) % 2 == 0)
            {
                ret.Append(Inner.Produce(rng, rulebook, parameters));
            }

            return ret.ToString();
        }

        public override string ToString()
        {
            if (ZeroAllowed)
            {
                return $"{Inner.ToString()}*";
            }
            else
            {
                return $"{Inner.ToString()}+";
            }
        }
    }
}
