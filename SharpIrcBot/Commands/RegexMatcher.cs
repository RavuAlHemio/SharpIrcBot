using System;
using System.Text.RegularExpressions;

namespace SharpIrcBot.Commands
{
    public class RegexMatcher : IArgumentMatcher, IEquatable<RegexMatcher>
    {
        public Regex Regex { get; }

        public RegexMatcher(string pattern)
        {
            Regex = new Regex(pattern, RegexOptions.Compiled);
        }

        public bool Match(string input, out object value)
        {
            Match match = Regex.Match(input);
            if (!match.Success || match.Index != 0)
            {
                value = null;
                return false;
            }
            else
            {
                value = match;
                return true;
            }
        }

        public bool Equals(RegexMatcher other)
        {
            if (other == null)
            {
                return false;
            }

            return
                this.Regex.ToString() == other.Regex.ToString()
                && this.Regex.Options == other.Regex.Options
                && this.Regex.MatchTimeout == other.Regex.MatchTimeout
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

            return this.Equals((RegexMatcher)obj);
        }

        public override int GetHashCode()
        {
            return unchecked(
                563 * this.Regex.ToString().GetHashCode()
                + 617 * this.Regex.Options.GetHashCode()
                + 653 * this.Regex.MatchTimeout.GetHashCode()
            );
        }
    }
}
