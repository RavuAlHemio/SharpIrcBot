using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SharpIrcBot.Plugins.Calc.AST;

namespace SharpIrcBot.Plugins.Calc
{
    public static class MathFuncs
    {
        private const decimal Epsilon = 0.0000000000000000000000000001m;

        public const decimal Pi = 3.1415926535897932384626433833m;
        public const decimal E = 2.7182818284590452353602874714m;
        public const decimal GoldenRatio = 1.6180339887498948482045868344m;

        public static decimal Sqrt(decimal radicand)
        {
            if (radicand < 0.0m)
            {
                throw new FunctionDomainException();
            }

            const int maxIterations = 64;

            decimal result = 1.0m;
            for (int i = 0; i < maxIterations; ++i)
            {
                decimal nextResult = (result + (radicand / result)) / 2.0m;
                if (Math.Abs(nextResult - result) < Epsilon)
                {
                    break;
                }
                result = nextResult;
            }

            return result;
        }

        public static decimal Sin(decimal t)
        {
            const int maxIterations = 64;

            while (t > 2*Pi)
            {
                t -= 2*Pi;
            }
            while (t < 0.0m)
            {
                t += 2*Pi;
            }

            // sum(0.., ( (-1)**n * t**(2*n + 1) / ((2*n + 1)!) ))

            decimal sign = 1.0m; // 1: +1
            decimal tpow = t; // 1: t
            decimal factorial = 1.0m; // 1: 1
            decimal sum = 0.0m;

            for (int i = 0; i < maxIterations; ++i)
            {
                decimal newSum = sum + (sign * tpow / factorial);

                if (Math.Abs(newSum - sum) < Epsilon)
                {
                    break;
                }

                sum = newSum;

                // +1, -1, +1, -1, ...
                sign = (sign == 1.0m) ? (-1.0m) : 1.0m;

                // t, t**3, t**5, t**7, ...
                tpow *= t * t;

                decimal newFactorial = factorial;
                bool retry;
                do
                {
                    retry = false;
                    try
                    {
                        newFactorial = factorial
                            * (2*i + 2)
                            * (2*i + 3);
                    }
                    catch (OverflowException)
                    {
                        factorial /= 2.0m;
                        tpow /= 2.0m;
                        retry = true;
                    }
                } while (retry);

                factorial = newFactorial;
            }

            return sum;
        }

        public static decimal Cos(decimal t)
            => Sin(Pi/2.0m - t);

        public static decimal Tan(decimal t)
            => Sin(t) / Cos(t);

        public static decimal Exp(decimal t)
        {
            const int maxIterations = 512;

            decimal tpow = 1; // 0: 1
            decimal factorial = 1.0m; // 0: 1
            decimal sum = 0.0m;

            for (int i = 0; i < maxIterations; ++i)
            {
                decimal newSum = sum + tpow / factorial;

                if (Math.Abs(newSum - sum) < Epsilon)
                {
                    break;
                }

                sum = newSum;

                bool retry;

                // t, t**2, t**3, t**4, t**5, ...
                decimal newTPow = tpow;
                decimal newFactorial = factorial;
                do
                {
                    retry = false;
                    try
                    {
                        newTPow = tpow * t;
                        newFactorial = factorial * (i + 1);
                    }
                    catch (OverflowException)
                    {
                        factorial /= 2.0m;
                        tpow /= 2.0m;
                        retry = true;
                    }
                } while (retry);
                tpow = newTPow;
                factorial = newFactorial;
            }

            return sum;
        }
    }
}
