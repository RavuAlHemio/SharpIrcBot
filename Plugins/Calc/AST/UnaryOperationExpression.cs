using System.Text;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class UnaryOperationExpression : Expression
    {
        public Operation Operation { get; }
        public Expression Operand { get; }

        public UnaryOperationExpression(Operation op, Expression operand)
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

        public override PrimitiveExpression Simplified(Grimoire grimoire)
        {
            PrimitiveExpression primOperand = Operand.Simplified(grimoire);

            switch (Operation)
            {
                case Operation.Negate:
                    return (primOperand.IsDecimal)
                        ? new PrimitiveExpression(-primOperand.DecimalValue)
                        : new PrimitiveExpression(-primOperand.LongValue)
                    ;
                default:
                    throw new SimplificationException($"Cannot handle unary operator {Operation}");
            }
        }
    }
}
