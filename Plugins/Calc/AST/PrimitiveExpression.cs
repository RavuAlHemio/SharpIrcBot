using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class PrimitiveExpression : Expression
    {
        public PrimitiveType Type { get; }
        public long LongValue { get; }
        public decimal DecimalValue { get; }
        public BigInteger BigIntegerValue { get; }

        public PrimitiveExpression(int index, int length, long longValue)
            : base(index, length)
        {
            Type = PrimitiveType.IntegerLong;
            LongValue = longValue;
            DecimalValue = 0.0m;
            BigIntegerValue = BigInteger.Zero;
        }

        public PrimitiveExpression(int index, int length, decimal decimalValue)
            : base(index, length)
        {
            Type = PrimitiveType.Decimal;
            LongValue = 0;
            DecimalValue = decimalValue;
            BigIntegerValue = BigInteger.Zero;
        }

        public PrimitiveExpression(int index, int length, BigInteger bigIntegerValue)
            : base(index, length)
        {
            Type = PrimitiveType.IntegerBig;
            LongValue = 0;
            DecimalValue = 0.0m;
            BigIntegerValue = bigIntegerValue;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case PrimitiveType.IntegerLong:
                    return LongValue.ToString(CultureInfo.InvariantCulture);
                case PrimitiveType.IntegerBig:
                    return BigIntegerValue.ToString(CultureInfo.InvariantCulture);
                case PrimitiveType.Decimal:
                    return DecimalValue.ToString(CultureInfo.InvariantCulture);
                default:
                    Debug.Fail($"Cannot handle unknown primitive type {Type}.");
                    return null;
            }
        }

        public override PrimitiveExpression Simplified(Grimoire grimoire, CalcTimer timer)
        {
            // nothing to simplify
            return this;
        }

        public PrimitiveExpression Repositioned(int index, int length)
        {
            switch (Type)
            {
                case PrimitiveType.IntegerLong:
                    return new PrimitiveExpression(index, length, LongValue);
                case PrimitiveType.IntegerBig:
                    return new PrimitiveExpression(index, length, BigIntegerValue);
                case PrimitiveType.Decimal:
                    return new PrimitiveExpression(index, length, DecimalValue);
                default:
                    Debug.Fail($"Cannot handle unknown primitive type {Type}.");
                    return null;
            }
        }

        public decimal ToDecimal()
        {
            switch (Type)
            {
                case PrimitiveType.IntegerLong:
                    return (decimal)LongValue;
                case PrimitiveType.IntegerBig:
                    return (decimal)BigIntegerValue;
                case PrimitiveType.Decimal:
                    return DecimalValue;
                default:
                    Debug.Fail($"Cannot convert primitive type {Type} to decimal.");
                    return decimal.Zero;
            }
        }

        public double ToDouble()
        {
            switch (Type)
            {
                case PrimitiveType.IntegerLong:
                    return (double)LongValue;
                case PrimitiveType.IntegerBig:
                    return (double)BigIntegerValue;
                case PrimitiveType.Decimal:
                    return (double)DecimalValue;
                default:
                    Debug.Fail($"Cannot convert primitive type {Type} to double.");
                    return double.NaN;
            }
        }
    }
}
