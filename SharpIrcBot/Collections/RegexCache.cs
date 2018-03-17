using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SharpIrcBot.Collections
{
    public class RegexCache
    {
        protected Dictionary<string, Regex> Regexes { get; set; }

        public RegexCache()
        {
            Regexes = new Dictionary<string, Regex>();
        }

        public Regex this[string pattern]
        {
            get
            {
                return GetOrAdd(pattern);
            }
        }

        public Regex GetOrAdd(string pattern)
        {
            Regex ret;
            if (Regexes.TryGetValue(pattern, out ret))
            {
                return ret;
            }

            ret = new Regex(pattern, RegexOptions.Compiled);
            Regexes[pattern] = ret;
            return ret;
        }

        public bool Contains(string pattern) => Regexes.ContainsKey(pattern);

        public void ReplaceAllWith(IEnumerable<string> newPatterns)
        {
            var newRegexes = new Dictionary<string, Regex>();
            foreach (string pattern in newPatterns)
            {
                Regex foundling;
                if (!Regexes.TryGetValue(pattern, out foundling))
                {
                    foundling = new Regex(pattern, RegexOptions.Compiled);
                }
                newRegexes[pattern] = foundling;
            }

            Regexes = newRegexes;
        }
    }
}
