using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class IdentProduction : Production
    {
        public string Identifier { get; }

        public IdentProduction(string identifier)
        {
            Identifier = identifier;
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            Rule rule = rulebook.GetRule(Identifier);
            return rule.Production.Produce(rng, rulebook, parameters);
        }

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
            // check if the referred rule exists
            if (!rulebook.RuleExists(Identifier))
            {
                errors.Add($"Rule does not exist: {Identifier}");
            }
            else
            {
                Rule rule = rulebook.GetRule(Identifier);
                if (rule.ParameterNames.Length != 0)
                {
                    errors.Add($"Rule {Identifier} has {rule.ParameterNames.Length} parameter(s) but we were given none");
                }
            }
        }

        public override string ToString()
        {
            return Identifier;
        }
    }
}
