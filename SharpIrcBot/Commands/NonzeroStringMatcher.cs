using System.Text.RegularExpressions;

namespace SharpIrcBot.Commands
{
    public class NonzeroStringMatcher : IArgumentMatcher
    {
        private static NonzeroStringMatcher _instance = null;

        public static NonzeroStringMatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NonzeroStringMatcher();
                }
                return _instance;
            }
        }

        protected NonzeroStringMatcher()
        {
        }

        public bool Match(string input, out object value)
        {
            if (input.Length > 0)
            {
                value = input;
                return true;
            }

            value = null;
            return false;
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return 677478093;
        }
    }
}
