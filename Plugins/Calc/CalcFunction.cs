using System;
using System.Collections.Immutable;
using SharpIrcBot.Plugins.Calc.AST;

namespace SharpIrcBot.Plugins.Calc
{
    public class CalcFunction
    {
        public string Name { get; }
        public int ArgumentCount { get; }
        public Func<ImmutableList<PrimitiveExpression>, PrimitiveExpression> Call { get; }

        public CalcFunction(
            string name, int argCount, Func<ImmutableList<PrimitiveExpression>, PrimitiveExpression> call
        )
        {
            Name = name;
            ArgumentCount = argCount;
            Call = call;
        }
    }
}
