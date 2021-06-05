using Antlr4.Runtime;
using Xunit;
using SharpIrcBot.Plugins.Calc;
using SharpIrcBot.Plugins.Calc.AST;
using SharpIrcBot.Plugins.Calc.Language;

namespace SharpIrcBot.Tests.CalcTests
{
    public class OperatorTests
    {
        static Expression ParseExpression(string expression)
        {
            var charStream = new AntlrInputStream(expression);

            var lexer = new CalcLangLexer(charStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(PanicErrorListener.Instance);

            var tokenStream = new CommonTokenStream(lexer);

            var parser = new CalcLangParser(tokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(PanicErrorListener.Instance);

            CalcLangParser.ExpressionContext exprContext = parser.fullExpression().expression();
            var visitor = new ASTGrowingVisitor(tokenStream);
            return visitor.Visit(exprContext);
        }

        static string Calculate(string expr)
        {
            Expression topExpression = ParseExpression(expr);
            Grimoire grimoire = Grimoire.CanonicalGrimoire;
            PrimitiveExpression simplifiedTop = topExpression.Simplified(grimoire, CalcTimer.NoTimeoutTimer);
            return simplifiedTop.ToString();
        }

        [Fact]
        public void TestPrecMulAdd()
        {
            Assert.Equal("10", Calculate("2 * 3 + 4"));
            Assert.Equal("14", Calculate("2 + 3 * 4"));
        }

        [Fact]
        public void TestAssocNegNeg()
        {
            Assert.Equal("2", Calculate("7 - 4 - 1"));
        }

        [Fact]
        public void TestAssocMulDiv()
        {
            Assert.Equal("2.25", Calculate("3/2*3/2"));
        }

        [Fact]
        public void TestAssocPowPow()
        {
            // right-associative (2**(3**3))
            Assert.Equal("134217728", Calculate("2**3**3"));
        }
    }
}
