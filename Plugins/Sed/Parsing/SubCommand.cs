using System.Text.RegularExpressions;

namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public class SubCommand : ITransformCommand
    {
        public Regex Pattern { get; set; }
        public string Replacement { get; set; }
        public int FirstMatch { get; set; }
        public bool ReplaceAll { get; set; }

        public SubCommand(Regex pattern, string replacement, int firstMatch, bool replaceAll)
        {
            Pattern = pattern;
            Replacement = replacement;
            FirstMatch = firstMatch;
            ReplaceAll = replaceAll;
        }

        public string Transform(string text)
        {
            if (FirstMatch < 0)
            {
                // match from end => we must count the matches first
                int matchCount = Pattern.Matches(text).Count;
                FirstMatch = matchCount + FirstMatch;
            }

            int matchIndex = -1;
            return Pattern.Replace(text, match =>
            {
                ++matchIndex;

                if (matchIndex < FirstMatch)
                {
                    // unchanged
                    return match.Value;
                }

                if (matchIndex > FirstMatch && !ReplaceAll)
                {
                    // unchanged
                    return match.Value;
                }

                return match.Result(Replacement);
            });
        }
    }
}
