using System;
using SharpIrcBot.Plugins.Calc.AST;

namespace SharpIrcBot.Plugins.Calc
{
    public class SimplificationException : Exception
    {
        public Expression Expression { get; }

        public SimplificationException(string message, Expression expr, Exception innerEx = null)
            : base(message, innerEx)
        {
            Expression = expr;
        }

        public SimplificationException(Expression expr, OverflowException innerEx)
            : this("Overflow.", expr, innerEx)
        {
        }

        public SimplificationException(Expression expr, DivideByZeroException innerEx)
            : this("Division by zero.", expr, innerEx)
        {
        }

        public SimplificationException(Expression expr, FunctionDomainException innerEx)
            : this("Undefined value.", expr, innerEx)
        {
        }

        public SimplificationException(Expression expr, TimeoutException innerEx)
            : this("Time limit reached.", expr, innerEx)
        {
        }
    }
}
