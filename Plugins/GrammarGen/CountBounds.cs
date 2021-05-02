using System;
using System.Numerics;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public struct CountBounds : IEquatable<CountBounds>
    {
        public readonly BigInteger Lower;
        public readonly BigInteger? Upper;

        public CountBounds(BigInteger lower, BigInteger? upper)
        {
            Lower = lower;
            Upper = upper;
        }

        public bool Equals(CountBounds other)
            => this.Lower == other.Lower
                && this.Upper == other.Upper;

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is CountBounds))
            {
                return false;
            }

            return this.Equals((CountBounds)obj);
        }

        public static bool operator==(CountBounds one, CountBounds other)
            => one.Equals(other);

        public static bool operator!=(CountBounds one, CountBounds other)
            => !(one == other);

        public override int GetHashCode() => (Lower, Upper).GetHashCode();

        public override string ToString()
        {
            return $"CountBounds({nameof(Lower)}: {Lower}, {nameof(Upper)}: {Upper})";
        }
    }
}
