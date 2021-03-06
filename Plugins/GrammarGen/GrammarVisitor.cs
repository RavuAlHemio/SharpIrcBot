using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharpIrcBot.Plugins.GrammarGen.AST;
using SharpIrcBot.Plugins.GrammarGen.Lang;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class GrammarVisitor : GrammarGenLangBaseVisitor<ASTNode>
    {
        public override ASTNode VisitGgrulebook(GrammarGenLangParser.GgrulebookContext context)
        {
            ImmutableDictionary<string, Rule> rules = context.ruledef()
                .Select(rule => (Rule)Visit(rule))
                .ToImmutableDictionary(rule => rule.Name, rule => rule);
            return new Rulebook(rules);
        }

        public override ASTNode VisitGgrule(GrammarGenLangParser.GgruleContext context)
        {
            string name = context.Identifier().GetText();
            var prod = (Production)Visit(context.ggproduction());
            return new Rule(name, prod);
        }

        public override ASTNode VisitParamrule(GrammarGenLangParser.ParamruleContext context)
        {
            string name = context.Identifier(0).GetText();
            ImmutableArray<string> paramNames = context.Identifier()
                .Skip(1)
                .Select(ident => ident.GetText())
                .ToImmutableArray();
            var prod = (Production)Visit(context.ggproduction());
            return new Rule(name, prod, paramNames);
        }

        public override ASTNode VisitGroup(GrammarGenLangParser.GroupContext context)
        {
            // placing groups in the AST ensures different probability behavior between
            // ("a" | "b" | "c")
            // and
            // ("a" | ("b" | "c"))
            var prod = (Production)Visit(context.ggproduction());
            return new GroupProduction(prod);
        }

        public override ASTNode VisitOpt(GrammarGenLangParser.OptContext context)
        {
            var prod = (Production)Visit(context.ggproduction());
            return new OptProduction(prod);
        }

        public override ASTNode VisitStar(GrammarGenLangParser.StarContext context)
        {
            var prod = (Production)Visit(context.sequenceElem());
            return new StarOrPlusProduction(true, prod);
        }

        public override ASTNode VisitAltern(GrammarGenLangParser.AlternContext context)
        {
            var alternatives = context.alternative();
            var bldr = ImmutableArray.CreateBuilder<Production>();
            foreach (var alternative in alternatives)
            {
                var prod = (Production)Visit(alternative);
                var alternProd = prod as AlternProduction;
                if (alternProd != null)
                {
                    // flatten!
                    bldr.AddRange(alternProd.Inners);
                }
                else
                {
                    bldr.Add(prod);
                }
            }

            return new AlternProduction(bldr.ToImmutable());
        }

        public override ASTNode VisitPlus(GrammarGenLangParser.PlusContext context)
        {
            var prod = (Production)Visit(context.sequenceElem());
            return new StarOrPlusProduction(false, prod);
        }

        public override ASTNode VisitSeq(GrammarGenLangParser.SeqContext context)
        {
            var seqElems = context.sequenceElem();
            var bldr = ImmutableArray.CreateBuilder<Production>();
            foreach (var seqElem in seqElems)
            {
                var prod = (Production)Visit(seqElem);
                var seqProd = prod as SeqProduction;
                if (seqProd != null)
                {
                    // flatten!
                    bldr.AddRange(seqProd.Inners);
                }
                else
                {
                    bldr.Add(prod);
                }
            }

            return new SeqProduction(bldr.ToImmutable());
        }

        public override ASTNode VisitIdent(GrammarGenLangParser.IdentContext context)
        {
            string identifier = context.Identifier().GetText();
            return new IdentProduction(identifier);
        }

        public override ASTNode VisitCall(GrammarGenLangParser.CallContext context)
        {
            string identifier = context.Identifier().GetText();
            ImmutableArray<Production> paramValues = context.ggproduction()
                .Select(param => (Production)Visit(param))
                .ToImmutableArray();
            return new CallProduction(identifier, paramValues);
        }

        public override ASTNode VisitStr(GrammarGenLangParser.StrContext context)
        {
            string escString = context.EscapedString().GetText();
            string unescString = UnescapeString(escString);
            return new StrProduction(unescString);
        }

        protected virtual string UnescapeString(string escapedString)
        {
            var sb = new StringBuilder();
            bool escaping = false;

            // ensure assumptions are true: string is wrapped with double quotes
            Debug.Assert(escapedString.Length >= 2);
            Debug.Assert(escapedString[0] == '"');
            Debug.Assert(escapedString[escapedString.Length - 1] == '"');

            foreach (char c in escapedString.Substring(1, escapedString.Length - 2))
            {
                if (escaping)
                {
                    sb.Append(c);
                }
                else if (c == '\\')
                {
                    escaping = true;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
