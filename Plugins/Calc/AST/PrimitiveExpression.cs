using System.Globalization;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class PrimitiveExpression : Expression
    {
        public bool IsDecimal { get; }
        public long LongValue { get; }
        public decimal DecimalValue { get; }

        public PrimitiveExpression(long longValue)
        {
            IsDecimal = false;
            LongValue = longValue;
            DecimalValue = 0.0m;
        }

        public PrimitiveExpression(decimal decimalValue)
        {
            IsDecimal = true;
            LongValue = 0;
            DecimalValue = decimalValue;
        }

        public override string ToString()
        {
            return IsDecimal
                ? DecimalValue.ToString(CultureInfo.InvariantCulture)
                : LongValue.ToString(CultureInfo.InvariantCulture);
        }

        public override PrimitiveExpression Simplified(Grimoire grimoire)
        {
            // nothing to simplify
            return this;
        }
    }
}
