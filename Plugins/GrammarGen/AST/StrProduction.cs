using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class StrProduction : Production
    {
        public string Str { get; }

        public StrProduction(string str)
        {
            Str = str;
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            return Str;
        }

        public override IEnumerable<string> ProduceAll(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            yield return Str;
        }

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
            // constant text doesn't depend on much
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in Str)
            {
                if (c == '\\')
                {
                    sb.Append("\\\\");
                }
                else if (c == '"')
                {
                    sb.Append("\\\"");
                }
                else
                {
                    sb.Append(c);
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
