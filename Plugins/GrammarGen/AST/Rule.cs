using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class Rule : ASTNode
    {
        public string Name { get; }
        public Production Production { get; }
        public ImmutableArray<string> ParameterNames { get; }

        public Rule(string name, Production production, ImmutableArray<string>? parameterNames = null)
        {
            Name = name;
            Production = production;
            ParameterNames = parameterNames ?? ImmutableArray<string>.Empty;
        }

        public override string ToString()
        {
            if (ParameterNames.Length > 0)
            {
                string paramNames = string.Join(", ", ParameterNames);
                return $"{Name}({paramNames}) : {Production} ;";
            }
            return $"{Name} : {Production} ;";
        }

        public void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
            // add our parameters with placeholder values to the rulebook
            var builder = ImmutableDictionary.CreateBuilder<string, Rule>();
            builder.AddRange(rulebook.Rules);
            foreach (string name in ParameterNames)
            {
                builder[name] = new Rule(name, new PlaceholderProduction());
            }
            var myRulebook = new Rulebook(builder.ToImmutable());

            Production.CollectSoundnessErrors(myRulebook, errors);
        }
    }
}
