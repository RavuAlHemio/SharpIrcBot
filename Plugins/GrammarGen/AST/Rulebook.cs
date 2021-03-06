using System.Collections.Immutable;
using SharpIrcBot.Plugins.GrammarGen.AST;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class Rulebook : ASTNode
    {
        public ImmutableDictionary<string, Rule> Rules { get; }

        public Rulebook(ImmutableDictionary<string, Rule> rules)
        {
            Rules = rules;
        }

        public Rule GetRule(string name) => Rules[name];
        public bool RuleExists(string name) => Rules.ContainsKey(name);
    }
}
