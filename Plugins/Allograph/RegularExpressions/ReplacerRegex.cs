using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SharpIrcBot.Plugins.Allograph.RegularExpressions.Internal;

namespace SharpIrcBot.Plugins.Allograph.RegularExpressions
{
    public class ReplacerRegex
    {
        public Regex Regex { get; }
        public string ReplacementString { get; }
        protected List<IPlaceholder> Placeholders { get; set; }

        public ReplacerRegex(string regex, string replacementString)
        {
            Regex = new Regex(regex, RegexOptions.Compiled);
            ReplacementString = replacementString;

            CompilePlaceholders();
        }

        public ReplacerRegex(Regex regex, string replacementString)
        {
            Regex = regex;
            ReplacementString = replacementString;

            CompilePlaceholders();
        }

        protected virtual void CompilePlaceholders()
        {
            Placeholders = PlaceholderCompiler.Compile(Regex, ReplacementString);
        }

        public virtual string Replace(string inputString, IDictionary<string, string> lookups = null)
        {
            return Regex.Replace(inputString, m => ReplaceMatch(inputString, m, lookups));
        }

        protected string ReplaceMatch(string inputString, Match match, IDictionary<string, string> lookups = null)
        {
            var lookupsCopy = (lookups == null)
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(lookups);

            var state = new ReplacementState
            {
                InputString = inputString,
                Regex = Regex,
                Match = match,
                Lookups = lookupsCopy
            };

            IEnumerable<string> replacedBits = Placeholders.Select(p => p.Replace(state));
            return string.Concat(replacedBits);
        }
    }
}
