using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SharpIrcBot.Commands
{
    public class LongMatcher : IArgumentMatcher, IEquatable<LongMatcher>
    {
        public bool AllowLeadingMinus { get; }
        public IFormatProvider FormatProvider { get; }

        public LongMatcher(bool allowLeadingMinus = false, IFormatProvider formatProvider = null)
        {
            AllowLeadingMinus = allowLeadingMinus;
            FormatProvider = (formatProvider == null)
                ? CultureInfo.InvariantCulture
                : formatProvider;
        }

        public bool Match(string input, out object value)
        {
            long ret;
            NumberStyles style = AllowLeadingMinus
                ? NumberStyles.AllowLeadingSign
                : NumberStyles.None;
            if (long.TryParse(input, style, FormatProvider, out ret))
            {
                value = ret;
                return true;
            }

            value = null;
            return false;
        }

        public bool Equals(LongMatcher other)
        {
            if (other == null)
            {
                return false;
            }

            return
                (this.AllowLeadingMinus == other.AllowLeadingMinus)
                && (this.FormatProvider == other.FormatProvider)
            ;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            return this.Equals((LongMatcher)obj);
        }

        public override int GetHashCode()
        {
            return unchecked(
                109 * AllowLeadingMinus.GetHashCode()
                + 313 * FormatProvider.GetHashCode()
            );
        }
    }
}
