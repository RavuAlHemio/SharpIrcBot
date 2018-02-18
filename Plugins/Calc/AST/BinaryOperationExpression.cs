using System;
using System.Text;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class BinaryOperationExpression : Expression
    {
        public Expression LeftSide { get; }
        public Operation Operation { get; }
        public Expression RightSide { get; }

        public BinaryOperationExpression(Expression leftSide, Operation op, Expression rightSide)
        {
            LeftSide = leftSide;
            Operation = op;
            RightSide = rightSide;
        }

        public override string ToString()
        {
            var ret = new StringBuilder();
            ret.Append('(');
            ret.Append(LeftSide);
            ret.Append(Operation.ToOperatorString());
            ret.Append(RightSide);
            ret.Append(')');
            return ret.ToString();
        }

        public override PrimitiveExpression Simplified(Grimoire grimoire)
        {
            PrimitiveExpression primLeft = LeftSide.Simplified(grimoire);
            PrimitiveExpression primRight = RightSide.Simplified(grimoire);

            // mixed types? coerce
            if (primLeft.IsDecimal && !primRight.IsDecimal)
            {
                primRight = new PrimitiveExpression((decimal)primRight.LongValue);
            }
            else if (!primLeft.IsDecimal && primRight.IsDecimal)
            {
                primLeft = new PrimitiveExpression((decimal)primLeft.LongValue);
            }

            switch (Operation)
            {
                case Operation.Add:
                    return (primLeft.IsDecimal)
                        ? new PrimitiveExpression(primLeft.DecimalValue + primRight.DecimalValue)
                        : new PrimitiveExpression(primLeft.LongValue + primRight.LongValue);
                case Operation.Divide:
                    return (primLeft.IsDecimal)
                        ? new PrimitiveExpression(primLeft.DecimalValue / primRight.DecimalValue)
                        : new PrimitiveExpression(primLeft.LongValue / primRight.LongValue);
                case Operation.Multiply:
                    return (primLeft.IsDecimal)
                        ? new PrimitiveExpression(primLeft.DecimalValue * primRight.DecimalValue)
                        : new PrimitiveExpression(primLeft.LongValue * primRight.LongValue);
                case Operation.Power:
                    return (primLeft.IsDecimal)
                        ? Pow(primLeft.DecimalValue, primRight.DecimalValue)
                        : Pow(primLeft.LongValue, primRight.LongValue);
                case Operation.Remainder:
                    return (primLeft.IsDecimal)
                        ? new PrimitiveExpression(primLeft.DecimalValue % primRight.DecimalValue)
                        : new PrimitiveExpression(primLeft.LongValue % primRight.LongValue);
                case Operation.Subtract:
                    return (primLeft.IsDecimal)
                        ? new PrimitiveExpression(primLeft.DecimalValue - primRight.DecimalValue)
                        : new PrimitiveExpression(primLeft.LongValue - primRight.LongValue);
                default:
                    throw new SimplificationException($"Cannot handle binary operator {Operation}.");
            }
        }

        public static PrimitiveExpression Pow(decimal baseDec, decimal exponentDec)
        {
            // FIXME: find a smarter way?
            return new PrimitiveExpression((decimal)Math.Pow((double)baseDec, (double)exponentDec));
        }

        public static PrimitiveExpression Pow(long baseLong, long exponentLong)
        {
            if (baseLong == 0)
            {
                if (exponentLong == 0)
                {
                    throw new SimplificationException("0**0 is undefined.");
                }
                else if (exponentLong < 0)
                {
                    throw new DivideByZeroException();
                }
            }

            if (baseLong == 1)
            {
                return new PrimitiveExpression((long)1);
            }

            if (exponentLong == 1)
            {
                return new PrimitiveExpression(baseLong);
            }

            long actualExponent = Math.Abs(exponentLong);
            long product = 1;
            for (long i = 1; i < actualExponent; ++i)
            {
                checked
                {
                    product *= baseLong;
                }
            }

            if (exponentLong < 0)
            {
                return new PrimitiveExpression(1.0m / ((decimal)product));
            }

            return new PrimitiveExpression(product);
        }
    }
}
