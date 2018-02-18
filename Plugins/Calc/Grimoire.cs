using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SharpIrcBot.Plugins.Calc.AST;

namespace SharpIrcBot.Plugins.Calc
{
    public class Grimoire
    {
        public ImmutableDictionary<string, PrimitiveExpression> Constants { get; }
        public ImmutableDictionary<string, CalcFunction> Functions { get; }

        private static Lazy<Grimoire> _canonicalGrimoire = new Lazy<Grimoire>(() => MakeCanonicalGrimore());
        private static Lazy<Grimoire> _emptyGrimoire = new Lazy<Grimoire>(() => new Grimoire(
            ImmutableDictionary<string, PrimitiveExpression>.Empty,
            ImmutableDictionary<string, CalcFunction>.Empty
        ));

        public static Grimoire CanonicalGrimoire => _canonicalGrimoire.Value;
        public static Grimoire Empty => _emptyGrimoire.Value;

        public Grimoire(
            IReadOnlyDictionary<string, PrimitiveExpression> constants,
            IReadOnlyDictionary<string, CalcFunction> functions
        )
        {
            Constants = constants.ToImmutableDictionary();
            Functions = functions.ToImmutableDictionary();
        }

        protected static Grimoire MakeCanonicalGrimore()
        {
            ImmutableDictionary<string, PrimitiveExpression>.Builder cBuilder
                = ImmutableDictionary.CreateBuilder<string, PrimitiveExpression>();
            ImmutableDictionary<string, CalcFunction>.Builder fBuilder
                = ImmutableDictionary.CreateBuilder<string, CalcFunction>();

            cBuilder.Add("pi", new PrimitiveExpression(MathFuncs.Pi));
            cBuilder.Add("e", new PrimitiveExpression(MathFuncs.E));
            cBuilder.Add("goldenRatio", new PrimitiveExpression(MathFuncs.GoldenRatio));
            cBuilder.Add("theAnswerToLifeTheUniverseAndEverything", new PrimitiveExpression(42));
            cBuilder.Add("numberOfHornsOnAUnicorn", new PrimitiveExpression(1));

            fBuilder.Add("sqrt", PrimitiveDecimalDecimalFunc("sqrt", MathFuncs.Sqrt));

            fBuilder.Add("sin", PrimitiveDecimalDecimalFunc("sin", MathFuncs.Sin));
            fBuilder.Add("cos", PrimitiveDecimalDecimalFunc("cos", MathFuncs.Cos));
            fBuilder.Add("tan", PrimitiveDecimalDecimalFunc("tan", MathFuncs.Tan));
            fBuilder.Add("exp", PrimitiveDecimalDecimalFunc("exp", MathFuncs.Exp));

            fBuilder.Add("asin", PrimitiveDoubleDoubleFunc("asin", Math.Asin));
            fBuilder.Add("acos", PrimitiveDoubleDoubleFunc("acos", Math.Acos));
            fBuilder.Add("atan", PrimitiveDoubleDoubleFunc("atan", Math.Atan));
            fBuilder.Add("atan2", PrimitiveDouble2DoubleFunc("atan2", Math.Atan2));
            fBuilder.Add("sinh", PrimitiveDoubleDoubleFunc("sinh", Math.Sinh));
            fBuilder.Add("cosh", PrimitiveDoubleDoubleFunc("cosh", Math.Cosh));
            fBuilder.Add("tanh", PrimitiveDoubleDoubleFunc("tanh", Math.Tanh));
            fBuilder.Add("ln", PrimitiveDoubleDoubleFunc("ln", Math.Log));
            fBuilder.Add("log10", PrimitiveDoubleDoubleFunc("log10", Math.Log10));
            fBuilder.Add("log", PrimitiveDouble2DoubleFunc("log", Math.Log));

            fBuilder.Add("ceil", PrimitiveDecimalDecimalFunc("ceil", Math.Ceiling));
            fBuilder.Add("floor", PrimitiveDecimalDecimalFunc("floor", Math.Floor));
            fBuilder.Add("round", PrimitiveDecimalDecimalFunc("round", Math.Round));
            fBuilder.Add("trunc", PrimitiveDecimalDecimalFunc("trunc", Math.Truncate));

            return new Grimoire(cBuilder.ToImmutable(), fBuilder.ToImmutable());
        }

        protected static CalcFunction PrimitiveDoubleDoubleFunc(string name, Func<double, double> innerFunc)
        {
            return new CalcFunction(
                name,
                1,
                (args) => new PrimitiveExpression(
                    (decimal)innerFunc(
                        args[0].IsDecimal ? (double)args[0].DecimalValue : (double)args[0].LongValue
                    )
                )
            );
        }

        protected static CalcFunction PrimitiveDecimalDecimalFunc(string name, Func<decimal, decimal> innerFunc)
        {
            return new CalcFunction(
                name,
                1,
                (args) => new PrimitiveExpression(
                    innerFunc(
                        args[0].IsDecimal ? args[0].DecimalValue : (decimal)args[0].LongValue
                    )
                )
            );
        }

        protected static CalcFunction PrimitiveDouble2DoubleFunc(string name, Func<double, double, double> innerFunc)
        {
            return new CalcFunction(
                name,
                2,
                (args) => new PrimitiveExpression(
                    (decimal)innerFunc(
                        args[0].IsDecimal ? (double)args[0].DecimalValue : (double)args[0].LongValue,
                        args[1].IsDecimal ? (double)args[1].DecimalValue : (double)args[1].LongValue
                    )
                )
            );
        }
    }
}
