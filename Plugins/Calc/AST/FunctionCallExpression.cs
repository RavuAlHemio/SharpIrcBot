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

        public FunctionCallExpression(string funcName, ImmutableList<Expression> args)
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

        public override PrimitiveExpression Simplified(Grimoire grimoire)
        {
            CalcFunction func;
            if (!grimoire.Functions.TryGetValue(FunctionName, out func))
            {
                throw new SimplificationException(
                    $"Unknown function '{FunctionName}'."
                );
            }
            if (func.ArgumentCount != Arguments.Count)
            {
                string expectedArgCountString = (func.ArgumentCount == 1)
                    ? $"{func.ArgumentCount} argument"
                    : $"{func.ArgumentCount} arguments"
                ;
                throw new SimplificationException(
                    $"Function '{FunctionName}' expects {expectedArgCountString}, got {Arguments.Count}."
                );
            }

            ImmutableList<PrimitiveExpression> simplifiedArgs = Arguments
                .ConvertAll(arg => arg.Simplified(grimoire));

            return func.Call.Invoke(simplifiedArgs);
        }
    }
}
