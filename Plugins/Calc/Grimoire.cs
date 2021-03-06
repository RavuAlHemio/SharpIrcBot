using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
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

            cBuilder.Add("pi", new PrimitiveExpression(-1, -1, MathFuncs.Pi));
            cBuilder.Add("e", new PrimitiveExpression(-1, -1, MathFuncs.E));
            cBuilder.Add("goldenRatio", new PrimitiveExpression(-1, -1, MathFuncs.GoldenRatio));
            cBuilder.Add("theAnswerToLifeTheUniverseAndEverything", new PrimitiveExpression(-1, -1, 42));
            cBuilder.Add("numberOfHornsOnAUnicorn", new PrimitiveExpression(-1, -1, 1));

            fBuilder.Add("sqrt", PrimitiveDecimalDecimalFunc("sqrt", MathFuncs.Sqrt));

            fBuilder.Add("sin", PrimitiveDecimalDecimalFunc("sin", MathFuncs.Sin));
            fBuilder.Add("cos", PrimitiveDecimalDecimalFunc("cos", MathFuncs.Cos));
            fBuilder.Add("tan", PrimitiveDecimalDecimalFunc("tan", MathFuncs.Tan));
            fBuilder.Add("exp", PrimitiveDecimalDecimalFunc("exp", MathFuncs.Exp));
            fBuilder.Add("deg2rad", PrimitiveDecimalDecimalFunc("deg2rad", MathFuncs.Deg2Rad));
            fBuilder.Add("deg2gon", PrimitiveDecimalDecimalFunc("deg2gon", MathFuncs.Deg2Gon));
            fBuilder.Add("rad2deg", PrimitiveDecimalDecimalFunc("rad2deg", MathFuncs.Rad2Deg));
            fBuilder.Add("rad2gon", PrimitiveDecimalDecimalFunc("rad2gon", MathFuncs.Rad2Gon));
            fBuilder.Add("gon2deg", PrimitiveDecimalDecimalFunc("gon2deg", MathFuncs.Gon2Deg));
            fBuilder.Add("gon2rad", PrimitiveDecimalDecimalFunc("gon2rad", MathFuncs.Gon2Rad));

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

            fBuilder.Add("ceil", PrimitiveDecimalDecimalToBigIntFunc("ceil", Math.Ceiling));
            fBuilder.Add("floor", PrimitiveDecimalDecimalToBigIntFunc("floor", Math.Floor));
            fBuilder.Add("round", PrimitiveDecimalDecimalToBigIntFunc("round", Math.Round));
            fBuilder.Add("trunc", PrimitiveDecimalDecimalToBigIntFunc("trunc", Math.Truncate));

            return new Grimoire(cBuilder.ToImmutable(), fBuilder.ToImmutable());
        }

        protected static CalcFunction PrimitiveDoubleDoubleFunc(string name, Func<double, double> innerFunc)
        {
            return new CalcFunction(
                name,
                1,
                (args) => new PrimitiveExpression(-1, -1, DoubleToDecimal(
                    innerFunc(args[0].ToDouble())
                ))
            );
        }

        protected static CalcFunction PrimitiveDecimalDecimalFunc(string name, Func<decimal, decimal> innerFunc)
        {
            return new CalcFunction(
                name,
                1,
                (args) => new PrimitiveExpression(
                    -1, -1,
                    innerFunc(args[0].ToDecimal())
                )
            );
        }

        protected static CalcFunction PrimitiveDecimalDecimalToBigIntFunc(string name, Func<decimal, decimal> innerFunc)
        {
            return new CalcFunction(
                name,
                1,
                (args) => new PrimitiveExpression(
                    -1, -1,
                    (BigInteger)innerFunc(args[0].ToDecimal())
                )
            );
        }

        protected static CalcFunction PrimitiveDouble2DoubleFunc(string name, Func<double, double, double> innerFunc)
        {
            return new CalcFunction(
                name,
                2,
                (args) => new PrimitiveExpression(-1, -1, DoubleToDecimal(
                    innerFunc(args[0].ToDouble(), args[1].ToDouble())
                ))
            );
        }

        protected static decimal DoubleToDecimal(double d)
        {
            if (double.IsInfinity(d))
            {
                throw new OverflowException();
            }
            if (double.IsNaN(d))
            {
                throw new FunctionDomainException();
            }
            return (decimal)d;
        }
    }
}
