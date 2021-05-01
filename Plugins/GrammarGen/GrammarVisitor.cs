using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using SharpIrcBot.Plugins.GrammarGen.AST;
using SharpIrcBot.Plugins.GrammarGen.Lang;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class GrammarVisitor : GrammarGenLangBaseVisitor<ASTNode>
    {
        public const int DefaultWeight = 50;

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

            int weight = DefaultWeight;
            if (context.weight() != null)
            {
                int testWeight;
                if (int.TryParse(context.weight().Number().GetText(), NumberStyles.None, CultureInfo.InvariantCulture, out testWeight))
                {
                    weight = testWeight;
                }
            }

            return new OptProduction(prod, weight);
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

            int weight = DefaultWeight;
            if (context.weight() != null)
            {
                int testWeight;
                if (int.TryParse(context.weight().Number().GetText(), NumberStyles.None, CultureInfo.InvariantCulture, out testWeight))
                {
                    weight = testWeight;
                }
            }

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

            return new SeqProduction(bldr.ToImmutable(), weight);
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
            int unicodeRemains = 0;
            long unicodeValue = 0;

            // ensure assumptions are true: string is wrapped with double quotes
            if (escapedString.Length < 2)
            {
                throw new ArgumentException(
                    $"{nameof(escapedString)} must be at least 2 characters long",
                    nameof(escapedString)
                );
            }
            if (escapedString[0] != '"')
            {
                throw new ArgumentException(
                    $"{nameof(escapedString)} must start with a quotation mark (U+0022)",
                    nameof(escapedString)
                );
            }
            if (escapedString[escapedString.Length - 1] != '"')
            {
                throw new ArgumentException(
                    $"{nameof(escapedString)} must end with a quotation mark (U+0022)",
                    nameof(escapedString)
                );
            }

            foreach (char c in escapedString.Substring(1, escapedString.Length - 2))
            {
                if (unicodeRemains > 0)
                {
                    long unicodeDigit;
                    if (c >= '0' && c <= '9')
                    {
                        unicodeDigit = c - '0';
                    }
                    else if (c >= 'a' && c <= 'f')
                    {
                        unicodeDigit = c - 'a' + 10;
                    }
                    else if (c >= 'A' && c <= 'F')
                    {
                        unicodeDigit = c - 'A' + 10;
                    }
                    else
                    {
                        throw new ArgumentException($"invalid hex digit within Unicode escape sequence: {c}", nameof(escapedString));
                    }

                    unicodeValue *= 16;
                    unicodeValue += unicodeDigit;

                    unicodeRemains--;
                    if (unicodeRemains == 0)
                    {
                        // end of character; append it
                        sb.Append(char.ConvertFromUtf32((int)unicodeValue));
                    }
                }
                else if (escaping)
                {
                    if (c == 'u')
                    {
                        // four hex-digit Unicode value
                        unicodeRemains = 4;
                        unicodeValue = 0;
                    }
                    else if (c == 'U')
                    {
                        // eight hex-digit Unicode value
                        unicodeRemains = 8;
                        unicodeValue = 0;
                    }
                    else
                    {
                        sb.Append(c);
                    }
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

            // ensure we didn't stop in the middle of a Unicode value
            // (the parser should actually forbid this)
            if (unicodeRemains != 0)
            {
                throw new ArgumentException(
                    $"{nameof(escapedString)} contains an incomplete Unicode escape sequence",
                    nameof(escapedString)
                );
            }
            Debug.Assert(unicodeRemains == 0);

            return sb.ToString();
        }
    }
}
