using System;

namespace SharpIrcBot.Commands
{
    public class WordMatchTaker : IArgumentTaker, IEquatable<WordMatchTaker>
    {
        public IArgumentMatcher Matcher { get; }
        public bool Required { get; }

        public WordMatchTaker(IArgumentMatcher matcher, bool required)
        {
            Matcher = matcher;
            Required = required;
        }

        public string Take(string input, out object value)
        {
            input = input.TrimStart();
            if (input.Length == 0)
            {
                value = null;

                if (Required)
                {
                    // fail
                    return null;
                }
                else
                {
                    // succeed with the empty string
                    return input;
                }
            }

            int i = 0;
            while (i < input.Length)
            {
                if (char.IsWhiteSpace(input, i))
                {
                    break;
                }

                // advance correctly
                if (char.IsSurrogatePair(input, i))
                {
                    i += 2;
                }
                else
                {
                    ++i;
                }
            }

            string command = input.Substring(0, i);
            if (Matcher.Match(command, out value))
            {
                // yay
                string rest = input.Substring(i);
                return rest;
            }
            else
            {
                value = null;
                return null;
            }
        }

        public bool Equals(WordMatchTaker other)
        {
            if (other == null)
            {
                return false;
            }

            return
                this.Matcher == other.Matcher
                && this.Required == other.Required
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

            return this.Equals((WordMatchTaker)obj);
        }

        public override int GetHashCode()
        {
            return
                1151 * Matcher.GetHashCode()
                + 1303 * Required.GetHashCode()
            ;
        }
    }
}
