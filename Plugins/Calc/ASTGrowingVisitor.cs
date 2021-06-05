using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using SharpIrcBot.Plugins.Calc.AST;
using SharpIrcBot.Plugins.Calc.Language;

namespace SharpIrcBot.Plugins.Calc
{
    public class ASTGrowingVisitor : CalcLangBaseVisitor<Expression>
    {
        protected BufferedTokenStream TokenStream { get; set; }

        public ASTGrowingVisitor(BufferedTokenStream tokenStream)
        {
            TokenStream = tokenStream;
        }

        public override Expression VisitAddSub([NotNull] CalcLangParser.AddSubContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            Operation op;
            switch (context.op.Text)
            {
                case "+": op = Operation.Add; break;
                case "-": op = Operation.Subtract; break;
                default:
                    throw new System.ArgumentException($"invalid AddSub operator: {context.op.Text}");
            }

            return new BinaryOperationExpression(
                index, length,
                Visit(context.expression(0)),
                op,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitMulDivRem([NotNull] CalcLangParser.MulDivRemContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            Operation op;
            switch (context.op.Text)
            {
                case "*": op = Operation.Multiply; break;
                case "/": op = Operation.Divide; break;
                case "//": op = Operation.IntegralDivide; break;
                case "%": op = Operation.Remainder; break;
                default:
                    throw new System.ArgumentException($"invalid MulDivRem operator: {context.op.Text}");
            }

            return new BinaryOperationExpression(
                index, length,
                Visit(context.expression(0)),
                op,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitPow([NotNull] CalcLangParser.PowContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new BinaryOperationExpression(
                index, length,
                Visit(context.expression(0)),
                Operation.Power,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitBAnd([NotNull] CalcLangParser.BAndContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new BinaryOperationExpression(
                index, length,
                Visit(context.expression(0)),
                Operation.BinaryAnd,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitBXor([NotNull] CalcLangParser.BXorContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new BinaryOperationExpression(
                index, length,
                Visit(context.expression(0)),
                Operation.BinaryXor,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitBOr([NotNull] CalcLangParser.BOrContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new BinaryOperationExpression(
                index, length,
                Visit(context.expression(0)),
                Operation.BinaryOr,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitFac([NotNull] CalcLangParser.FacContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new UnaryOperationExpression(
                index, length,
                Operation.Factorial,
                Visit(context.expression())
            );
        }

        public override Expression VisitNeg([NotNull] CalcLangParser.NegContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new UnaryOperationExpression(
                index, length,
                Operation.Negate,
                Visit(context.expression())
            );
        }

        public override Expression VisitInt([NotNull] CalcLangParser.IntContext context)
        {
            // strip off the underscores
            string text = context.Integer().GetText().Replace("_", "");

            int numBase = 10;
            if (text.StartsWith("0x"))
            {
                text = text.Substring(2);
                numBase = 16;
            }
            else if (text.StartsWith("0o"))
            {
                text = text.Substring(2);
                numBase = 8;
            }
            else if (text.StartsWith("0b"))
            {
                text = text.Substring(2);
                numBase = 2;
            }

            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            BigInteger result = HornerScheme(text, numBase);
            if (result <= long.MaxValue)
            {
                return new PrimitiveExpression(index, length, (long)result);
            }

            return new PrimitiveExpression(index, length, result);
        }

        public static BigInteger HornerScheme(string number, int numBase)
        {
            Debug.Assert(numBase > 0 && numBase <= 62);

            BigInteger ret = BigInteger.Zero;
            foreach (char c in number)
            {
                ret *= numBase;
                if (c == '0')
                {
                    continue;
                }

                if (c >= '0' && c <= '9')
                {
                    int digitValue = (c - '0');
                    Debug.Assert(digitValue < numBase);
                    ret += digitValue;
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    int digitValue = (c - 'A') + 10;
                    Debug.Assert(digitValue < numBase);
                    ret += digitValue;
                }
                else if (c >= 'a' && c <= 'z')
                {
                    int digitValue;
                    if (numBase <= 36)
                    {
                        // assume equivalence between capital and lowercase letters
                        digitValue = (c - 'a') + 10;
                    }
                    else
                    {
                        // assume lowercase letters follow uppercase
                        digitValue = (c - 'a') + 10 + 26;
                    }
                    Debug.Assert(digitValue < numBase);
                    ret += digitValue;
                }
            }

            return ret;
        }

        public override Expression VisitDec([NotNull] CalcLangParser.DecContext context)
        {
            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new PrimitiveExpression(
                index, length,
                decimal.Parse(context.Decimal().GetText(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
            );
        }

        public override Expression VisitCst([NotNull] CalcLangParser.CstContext context)
        {
            string name = context.Identifier().GetText();

            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new ConstantReferenceExpression(
                index, length,
                name
            );
        }

        public override Expression VisitFunc([NotNull] CalcLangParser.FuncContext context)
        {
            string name = context.Identifier().GetText();

            ImmutableList<Expression>.Builder arguments = ImmutableList.CreateBuilder<Expression>();
            CalcLangParser.ArglistContext argCtx = context.arglist();
            while (argCtx != null)
            {
                arguments.Add(Visit(argCtx.expression()));
                argCtx = argCtx.arglist();
            }

            (int index, int length) = ExpressionIndexAndLength(context.SourceInterval);

            return new FunctionCallExpression(
                index, length,
                name,
                arguments.ToImmutable()
            );
        }

        public override Expression VisitParens([NotNull] CalcLangParser.ParensContext context)
        {
            return Visit(context.expression());
        }

        protected (int index, int length) ExpressionIndexAndLength(Interval sourceInterval)
        {
            int index = TokenStream.Get(sourceInterval.a).StartIndex;
            int length = TokenStream.Get(sourceInterval.b).StopIndex - index + 1;
            return (index, length);
        }
    }
}
