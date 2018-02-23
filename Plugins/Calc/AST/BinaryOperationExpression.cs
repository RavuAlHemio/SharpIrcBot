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
                        primRight = new PrimitiveExpression(primRight.Index, primRight.Length, (decimal)primRight.DecimalValue);
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
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.LongValue + primRight.LongValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.BigIntegerValue + primRight.BigIntegerValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.Decimal)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.DecimalValue + primRight.DecimalValue
                            );
                        }
                        break;
                    case Operation.Divide:
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.LongValue / primRight.LongValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.BigIntegerValue / primRight.BigIntegerValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.Decimal)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.DecimalValue / primRight.DecimalValue
                            );
                        }
                        break;
                    case Operation.Multiply:
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.LongValue * primRight.LongValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.BigIntegerValue * primRight.BigIntegerValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.Decimal)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.DecimalValue * primRight.DecimalValue
                            );
                        }
                        break;
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
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.LongValue % primRight.LongValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.BigIntegerValue % primRight.BigIntegerValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.Decimal)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.DecimalValue % primRight.DecimalValue
                            );
                        }
                        break;
                    case Operation.Subtract:
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.LongValue - primRight.LongValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.BigIntegerValue - primRight.BigIntegerValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.Decimal)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.DecimalValue - primRight.DecimalValue
                            );
                        }
                        break;
                    case Operation.BinaryAnd:
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.LongValue & primRight.LongValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.BigIntegerValue & primRight.BigIntegerValue
                            );
                        }
                        break;
                    case Operation.BinaryOr:
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.LongValue | primRight.LongValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.BigIntegerValue | primRight.BigIntegerValue
                            );
                        }
                        break;
                    case Operation.BinaryXor:
                        if (primLeft.Type == PrimitiveType.IntegerLong)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.LongValue ^ primRight.LongValue
                            );
                        }
                        else if (primLeft.Type == PrimitiveType.IntegerBig)
                        {
                            return new PrimitiveExpression(
                                newIndex,
                                newLength,
                                primLeft.BigIntegerValue ^ primRight.BigIntegerValue
                            );
                        }
                        break;
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
