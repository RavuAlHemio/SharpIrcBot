using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class FunctionCallExpression : Expression
    {
        public string FunctionName { get; }
        public ImmutableList<Expression> Arguments { get; }

        public FunctionCallExpression(int index, int length, string funcName, ImmutableList<Expression> args)
            : base(index, length)
        {
            FunctionName = funcName;
            Arguments = args;
        }

        public override string ToString()
        {
            var ret = new StringBuilder();
            ret.Append(FunctionName);
            ret.Append('(');
            if (Arguments.Count > 0)
            {
                ret.Append(Arguments[0]);
                for (int i = 1; i < Arguments.Count; ++i)
                {
                    ret.Append(", ");
                    ret.Append(Arguments[i]);
                }
            }
            ret.Append(')');
            return ret.ToString();
        }

        public override PrimitiveExpression Simplified(Grimoire grimoire, CalcTimer timer)
        {
            timer.ThrowIfTimedOut();

            CalcFunction func;
            if (!grimoire.Functions.TryGetValue(FunctionName, out func))
            {
                throw new SimplificationException(
                    $"Unknown function '{FunctionName}'.",
                    this
                );
            }
            if (func.ArgumentCount != Arguments.Count)
            {
                string expectedArgCountString = (func.ArgumentCount == 1)
                    ? $"{func.ArgumentCount} argument"
                    : $"{func.ArgumentCount} arguments"
                ;
                throw new SimplificationException(
                    $"Function '{FunctionName}' expects {expectedArgCountString}, got {Arguments.Count}.",
                    this
                );
            }

            ImmutableList<PrimitiveExpression> simplifiedArgs = Arguments
                .ConvertAll(arg => arg.Simplified(grimoire, timer));

            timer.ThrowIfTimedOut();

            PrimitiveExpression result;
            try
            {
                result = func.Call.Invoke(simplifiedArgs);
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
            return result.Repositioned(Index, Length);
        }
    }
}
