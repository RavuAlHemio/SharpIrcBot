using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class CallProduction : Production
    {
        public string Identifier { get; }
        public ImmutableArray<Production> Parameters { get; }

        public CallProduction(string identifier, ImmutableArray<Production> parameters)
        {
            Identifier = identifier;
            Parameters = parameters;
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            Rule rule = rulebook.GetRule(Identifier);

            // add the parameters to the rulebook
            var builder = ImmutableDictionary.CreateBuilder<string, Rule>();
            builder.AddRange(rulebook.Rules);
            Debug.Assert(rule.ParameterNames.Length == Parameters.Length);
            for (int i = 0; i < rule.ParameterNames.Length; i++)
            {
                builder[rule.ParameterNames[i]] = new Rule(rule.ParameterNames[i], Parameters[i]);
            }
            Rulebook subtreeRulebook = new Rulebook(builder.ToImmutable());

            return rule.Production.Produce(rng, subtreeRulebook, parameters);
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
                if (rule.ParameterNames.Length != Parameters.Length)
                {
                    errors.Add($"Rule {Identifier} has {rule.ParameterNames.Length} parameter(s) but we were given {Parameters.Length}");
                }
            }

            // check soundness of parameter values
            foreach (Production parameter in Parameters)
            {
                parameter.CollectSoundnessErrors(rulebook, errors);
            }
        }

        public override string ToString()
        {
            return Identifier;
        }
    }
}
