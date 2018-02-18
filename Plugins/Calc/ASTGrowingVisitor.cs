using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Antlr4.Runtime.Misc;
using SharpIrcBot.Plugins.Calc.AST;
using SharpIrcBot.Plugins.Calc.Language;

namespace SharpIrcBot.Plugins.Calc
{
    public class ASTGrowingVisitor : CalcLangBaseVisitor<Expression>
    {
        public override Expression VisitAdd([NotNull] CalcLangParser.AddContext context)
        {
            return new BinaryOperationExpression(
                Visit(context.expression(0)),
                Operation.Add,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitDiv([NotNull] CalcLangParser.DivContext context)
        {
            return new BinaryOperationExpression(
                Visit(context.expression(0)),
                Operation.Divide,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitMul([NotNull] CalcLangParser.MulContext context)
        {
            return new BinaryOperationExpression(
                Visit(context.expression(0)),
                Operation.Multiply,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitRem([NotNull] CalcLangParser.RemContext context)
        {
            return new BinaryOperationExpression(
                Visit(context.expression(0)),
                Operation.Remainder,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitPow([NotNull] CalcLangParser.PowContext context)
        {
            return new BinaryOperationExpression(
                Visit(context.expression(0)),
                Operation.Power,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitSub([NotNull] CalcLangParser.SubContext context)
        {
            return new BinaryOperationExpression(
                Visit(context.expression(0)),
                Operation.Subtract,
                Visit(context.expression(1))
            );
        }

        public override Expression VisitNeg([NotNull] CalcLangParser.NegContext context)
        {
            return new UnaryOperationExpression(Operation.Negate, Visit(context.expression()));
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

            return new PrimitiveExpression(HornerScheme(text, numBase));
        }

        public static long HornerScheme(string number, int numBase)
        {
            Debug.Assert(numBase > 0 && numBase <= 62);

            long ret = 0;
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
            return new PrimitiveExpression(
                decimal.Parse(context.Decimal().GetText(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
            );
        }

        public override Expression VisitCst([NotNull] CalcLangParser.CstContext context)
        {
            string name = context.Identifier().GetText();

            return new ConstantReferenceExpression(name);
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

            return new FunctionCallExpression(name, arguments.ToImmutable());
        }

        public override Expression VisitParens([NotNull] CalcLangParser.ParensContext context)
        {
            return Visit(context.expression());
        }
    }
}
