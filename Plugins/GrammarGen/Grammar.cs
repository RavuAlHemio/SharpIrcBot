using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using SharpIrcBot.Plugins.GrammarGen.AST;
using SharpIrcBot.Plugins.GrammarGen.Lang;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class Grammar
    {
        public Rulebook Rules { get; }
        public string StartRule { get; }

        protected virtual Rulebook Parse(string definition)
        {
            var charStream = new AntlrInputStream(definition);

            var lexer = new GrammarGenLangLexer(charStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(PanicErrorListener.Instance);

            var tokenStream = new CommonTokenStream(lexer);

            var parser = new GrammarGenLangParser(tokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(PanicErrorListener.Instance);

            GrammarGenLangParser.GgrulebookContext rulebookContext = parser.ggrulebook();
            var visitor = new GrammarVisitor();

            return (Rulebook)visitor.Visit(rulebookContext);
        }

        public Grammar(string definition, string startRule, Rulebook predefinedRules = null)
        {
            // parse the definition to get a rulebook
            Rulebook parsedRules = Parse(definition);

            // add the predefined rules
            Rulebook rules = parsedRules;
            if (predefinedRules != null)
            {
                var newRules = ImmutableDictionary.CreateBuilder<string, Rule>();
                newRules.AddRange(parsedRules.Rules);
                newRules.AddRange(predefinedRules.Rules);
                rules = new Rulebook(newRules.ToImmutable());
            }

            // check if the start rule exists
            if (!rules.RuleExists(startRule))
            {
                throw new GrammarException($"start rule does not exist: {startRule}");
            }

            // check all rules for soundness
            var ruleErrors = new List<string>();
            foreach (Rule rule in rules.Rules.Values)
            {
                var thisRuleErrors = new List<string>();
                rule.CollectSoundnessErrors(rules, thisRuleErrors);
                ruleErrors.AddRange(
                    thisRuleErrors
                        .Select(e => $"error in rule {rule.Name}: {e}")
                );
            }
            if (ruleErrors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append($"{ruleErrors.Count} soundness violation(s) in rulebook:");
                foreach (string err in ruleErrors)
                {
                    sb.Append("\r\n");
                    sb.Append(err);
                }
                throw new GrammarException(sb.ToString());
            }

            // I guess we're OK
            Rules = rules;
            StartRule = startRule;
        }

        public string Generate(Random rng = null, ImmutableDictionary<string, object> parameters = null)
        {
            if (rng == null)
            {
                rng = new Random();
            }
            if (parameters == null)
            {
                parameters = ImmutableDictionary<string, object>.Empty;
            }

            Rule startRule = Rules.GetRule(StartRule);
            Production startProd = startRule.Production;
            string result = startProd.Produce(rng, Rules, parameters);
            return result;
        }
    }
}
