using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class BinaryOperationExpression : Expression
    {
        public Expression LeftSide { get; }
        public Operation Operation { get; }
        public Expression RightSide { get; }

        public BinaryOperationExpression(int index, int length, Expression leftSide, Operation op, Expression rightSide)
            : base(index, length)
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

        public override PrimitiveExpression Simplified(Grimoire grimoire, CalcTimer timer)
        {
            timer.ThrowIfTimedOut();

            PrimitiveExpression primLeft = LeftSide.Simplified(grimoire, timer);
            timer.ThrowIfTimedOut();
            PrimitiveExpression primRight = RightSide.Simplified(grimoire, timer);
            timer.ThrowIfTimedOut();

            // type check
            switch (Operation)
            {
                case Operation.BinaryAnd:
                case Operation.BinaryOr:
                case Operation.BinaryXor:
                    if (primLeft.Type == PrimitiveType.Decimal || primRight.Type == PrimitiveType.Decimal)
                    {
                        throw new SimplificationException(
                            "Cannot perform a bitwise operation on a floating-point number.",
                            this
                        );
                    }
                    break;
                default:
                    break;
            }

            try
            {
                // mixed types? coerce
                if (primLeft.Type == PrimitiveType.IntegerLong)
                {
                    if (primRight.Type == PrimitiveType.IntegerBig)
                    {
                        // IntegerLong < IntegerBig
                        primLeft = new PrimitiveExpression(primLeft.Index, primLeft.Length, new BigInteger(primLeft.LongValue));
                    }
                    else if (primRight.Type == PrimitiveType.Decimal)
                    {
                        // IntegerLong < Decimal
                        primLeft = new PrimitiveExpression(primLeft.Index, primLeft.Length, (decimal)primLeft.LongValue);
                    }
                }
                else if (primLeft.Type == PrimitiveType.IntegerBig)
                {
                    if (primRight.Type == PrimitiveType.IntegerLong)
                    {
                        // IntegerBig > IntegerLong
                        primRight = new PrimitiveExpression(primRight.Index, primRight.Length, new BigInteger(primRight.LongValue));
                    }
                    else if (primRight.Type == PrimitiveType.Decimal)
                    {
                        // IntegerBig < Decimal
                        primLeft = new PrimitiveExpression(primLeft.Index, primLeft.Length, (decimal)primLeft.BigIntegerValue);
                    }
                }
                else if (primLeft.Type == PrimitiveType.Decimal)
                {
                    if (primRight.Type == PrimitiveType.IntegerLong)
                    {
                        // Decimal > IntegerLong
                        primRight = new PrimitiveExpression(primRight.Index, primRight.Length, (decimal)primRight.LongValue);
                    }
                    else if (primRight.Type == PrimitiveType.IntegerBig)
                    {
                        // Decimal > IntegerBig
                        primRight = new PrimitiveExpression(primRight.Index, primRight.Length, (decimal)primRight.BigIntegerValue);
                    }
                }
            }
            catch (OverflowException ex)
            {
                throw new SimplificationException(this, ex);
            }
            catch (DivideByZeroException ex)
            {
                throw new SimplificationException(this, ex);
            }
            catch (FunctionDomainException ex)
            {
                throw new SimplificationException(this, ex);
            }
            catch (TimeoutException ex)
            {
                throw new SimplificationException(this, ex);
            }

            timer.ThrowIfTimedOut();

            Debug.Assert(primLeft.Type == primRight.Type);

            int newIndex = primLeft.Index;
            int newLength = primRight.Index + primRight.Length - primLeft.Index;

            try
            {
                switch (Operation)
                {
                    case Operation.Add:
                        return BinaryOp(
                            newIndex, newLength, primLeft, primRight,
                            (a, b) => checked(a + b),
                            (a, b) => checked(a + b),
                            (a, b) => checked(a + b)
                        );
                    case Operation.Divide:
                        return new PrimitiveExpression(
                            newIndex, newLength,
                            checked(primLeft.ToDecimal() / primRight.ToDecimal())
                        );
                    case Operation.IntegralDivide:
                        return BinaryOp(
                            newIndex, newLength, primLeft, primRight,
                            (a, b) => checked(a / b),
                            (a, b) => checked(a / b),
                            (a, b) => Math.Truncate(checked(a / b))
                        );
                    case Operation.Multiply:
                        return BinaryOp(
                            newIndex, newLength, primLeft, primRight,
                            (a, b) => checked(a * b),
                            (a, b) => checked(a * b),
                            (a, b) => checked(a * b)
                        );
                    case Operation.Power:
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return Pow(newIndex, newLength, primLeft.LongValue, primRight.LongValue, timer);
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return Pow(newIndex, newLength, primLeft.BigIntegerValue, primRight.BigIntegerValue, timer);
                        }
                        else if (primLeft.Type == PrimitiveType.Decimal)
                        {
                            return Pow(newIndex, newLength, primLeft.DecimalValue, primRight.DecimalValue, timer);
                        }
                        break;
                    case Operation.Remainder:
                        return BinaryOp(
                            newIndex, newLength, primLeft, primRight,
                            (a, b) => checked(a % b),
                            (a, b) => checked(a % b),
                            (a, b) => checked(a % b)
                        );
                    case Operation.Subtract:
                        return BinaryOp(
                            newIndex, newLength, primLeft, primRight,
                            (a, b) => checked(a - b),
                            (a, b) => checked(a - b),
                            (a, b) => checked(a - b)
                        );
                    case Operation.BinaryAnd:
                        return BinaryOp(
                            newIndex, newLength, primLeft, primRight,
                            (a, b) => checked(a & b),
                            (a, b) => checked(a & b),
                            null
                        );
                    case Operation.BinaryOr:
                        return BinaryOp(
                            newIndex, newLength, primLeft, primRight,
                            (a, b) => checked(a | b),
                            (a, b) => checked(a | b),
                            null
                        );
                    case Operation.BinaryXor:
                        return BinaryOp(
                            newIndex, newLength, primLeft, primRight,
                            (a, b) => checked(a ^ b),
                            (a, b) => checked(a ^ b),
                            null
                        );
                }
            }
            catch (OverflowException ex)
            {
                throw new SimplificationException(this, ex);
            }
            catch (DivideByZeroException ex)
            {
                throw new SimplificationException(this, ex);
            }
            catch (FunctionDomainException ex)
            {
                throw new SimplificationException(this, ex);
            }
            catch (TimeoutException ex)
            {
                throw new SimplificationException(this, ex);
            }

            throw new SimplificationException($"Cannot handle binary operator {Operation}.", this);
        }

        protected static PrimitiveExpression BinaryOp(
            int index, int length, PrimitiveExpression leftOperand, PrimitiveExpression rightOperand,
            Func<long, long, long> longOperation, Func<BigInteger, BigInteger, BigInteger> bigOperation,
            Func<decimal, decimal, decimal> decimalOperation
        )
        {
            Debug.Assert(leftOperand.Type == rightOperand.Type);

            if (leftOperand.Type == PrimitiveType.IntegerLong)
            {
                // try natively long
                try
                {
                    return new PrimitiveExpression(
                        index, length, longOperation(leftOperand.LongValue, rightOperand.LongValue)
                    );
                }
                catch (OverflowException)
                {
                }

                // try promoting to BigInteger
                try
                {
                    return new PrimitiveExpression(
                        index, length, bigOperation(leftOperand.LongValue, rightOperand.LongValue)
                    );
                }
                catch (OverflowException)
                {
                }

                // last attempt: decimal
                return new PrimitiveExpression(
                    index, length, decimalOperation(leftOperand.LongValue, rightOperand.LongValue)
                );
            }
            else if (leftOperand.Type == PrimitiveType.IntegerBig)
            {
                // try natively BigInteger
                try
                {
                    return new PrimitiveExpression(
                        index, length, bigOperation(leftOperand.BigIntegerValue, rightOperand.BigIntegerValue)
                    );
                }
                catch (OverflowException)
                {
                }

                // last attempt: decimal
                return new PrimitiveExpression(
                    index, length,
                    decimalOperation(
                        checked((decimal)leftOperand.BigIntegerValue),
                        checked((decimal)rightOperand.BigIntegerValue)
                    )
                );
            }
            else if (leftOperand.Type == PrimitiveType.Decimal)
            {
                // just go decimal
                return new PrimitiveExpression(
                    index, length,
                    decimalOperation(leftOperand.DecimalValue, rightOperand.DecimalValue)
                );
            }
            else
            {
                Debug.Fail($"Unexpected primitive expression type '{leftOperand.Type}'.");
                return new PrimitiveExpression(index, length, (long)0);
            }
        }

        public static PrimitiveExpression Pow(int index, int length, decimal baseDec, decimal exponentDec, CalcTimer timer)
        {
            // FIXME: find a smarter way?
            return new PrimitiveExpression(index, length, (decimal)Math.Pow((double)baseDec, (double)exponentDec));
        }

        public PrimitiveExpression Pow(int index, int length, long baseLong, long exponentLong, CalcTimer timer)
        {
            PrimitiveExpression pex = Pow(index, length, (BigInteger)baseLong, (BigInteger)exponentLong, timer);

            if (
                pex.Type == PrimitiveType.IntegerBig
                && pex.BigIntegerValue >= long.MinValue
                && pex.BigIntegerValue <= long.MaxValue
            )
            {
                // don't promote where unnecessary
                return new PrimitiveExpression(index, length, (long)pex.BigIntegerValue);
            }
            return pex;
        }

        public PrimitiveExpression Pow(int index, int length, BigInteger baseBig, BigInteger exponentBig, CalcTimer timer)
        {
            if (baseBig.IsZero)
            {
                if (exponentBig.IsZero)
                {
                    throw new SimplificationException("0**0 is undefined.", this);
                }
                else if (exponentBig < BigInteger.Zero)
                {
                    throw new DivideByZeroException();
                }
            }

            if (baseBig.IsOne)
            {
                return new PrimitiveExpression(index, length, BigInteger.One);
            }

            if (exponentBig.IsOne)
            {
                return new PrimitiveExpression(index, length, baseBig);
            }

            timer.ThrowIfTimedOut();

            BigInteger actualExponent = BigInteger.Abs(exponentBig);
            BigInteger product = BigInteger.One;
            for (long i = 1; i < actualExponent; ++i)
            {
                timer.ThrowIfTimedOut();

                checked
                {
                    product *= baseBig;
                }
            }

            if (exponentBig.Sign < 0)
            {
                return new PrimitiveExpression(index, length, 1.0m / ((decimal)product));
            }

            return new PrimitiveExpression(index, length, product);
        }
    }
}
