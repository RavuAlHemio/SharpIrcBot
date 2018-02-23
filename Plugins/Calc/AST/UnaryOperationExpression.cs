using System.Text;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class UnaryOperationExpression : Expression
    {
        public Operation Operation { get; }
        public Expression Operand { get; }

        public UnaryOperationExpression(int index, int length, Operation op, Expression operand)
            : base(index, length)
        {
            Operation = op;
            Operand = operand;
        }

        public override string ToString()
        {
            var ret = new StringBuilder();
            ret.Append('(');
            ret.Append(Operation.ToOperatorString());
            ret.Append(Operand);
            ret.Append(')');
            return ret.ToString();
        }

        public override PrimitiveExpression Simplified(Grimoire grimoire, CalcTimer timer)
        {
            timer.ThrowIfTimedOut();

            PrimitiveExpression primOperand = Operand.Simplified(grimoire, timer);

            switch (Operation)
            {
                case Operation.Negate:
                    if (primOperand.Type == PrimitiveType.IntegerLong)
                    {
                        return new PrimitiveExpression(Index, Length, -primOperand.LongValue);
                    }
                    else if (primOperand.Type == PrimitiveType.IntegerBig)
                    {
                        return new PrimitiveExpression(Index, Length, -primOperand.BigIntegerValue);
                    }
                    else if (primOperand.Type == PrimitiveType.Decimal)
                    {
                        return new PrimitiveExpression(Index, Length, -primOperand.DecimalValue);
                    }
                    break;
                default:
                    break;
            }

            throw new SimplificationException($"Cannot handle unary operator {Operation}.", this);
        }
    }
}
