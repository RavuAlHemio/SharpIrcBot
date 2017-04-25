using System;

namespace SharpIrcBot.Commands
{
    public class VerbatimTaker : IArgumentTaker, IEquatable<VerbatimTaker>
    {
        public string StringToMatch { get; }
        public bool Required { get; }
        public bool TrimInitialWhitespace { get; }

        public VerbatimTaker(string stringToMatch, bool required = true, bool trimInitialWhitespace = true)
        {
            StringToMatch = stringToMatch;
            Required = required;
        }

        public string Take(string input, out object value)
        {
            string myInput = TrimInitialWhitespace
                ? input.TrimStart()
                : input;

            if (myInput.StartsWith(StringToMatch))
            {
                value = StringToMatch;
                return myInput.Substring(StringToMatch.Length);
            }
            else
            {
                value = null;

                if (Required)
                {
                    return null;
                }
                else
                {
                    // pretend we got what we wanted
                    return input;
                }
            }
        }

        public bool Equals(VerbatimTaker other)
        {
            if (other == null)
            {
                return false;
            }

            return
                this.StringToMatch == other.StringToMatch
                && this.Required == other.Required
                && this.TrimInitialWhitespace == other.TrimInitialWhitespace
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

            return this.Equals((VerbatimTaker)obj);
        }

        public override int GetHashCode()
        {
            return unchecked(
                1009 * StringToMatch.GetHashCode()
                + 1033 * Required.GetHashCode()
                + 1151 * TrimInitialWhitespace.GetHashCode()
            );
        }
    }
}
