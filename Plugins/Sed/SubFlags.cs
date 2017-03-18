using System.Text.RegularExpressions;

namespace SharpIrcBot.Plugins.Sed
{
    public class SubFlags
    {
        public RegexOptions Options { get; set; }
        public int FirstMatch { get; set; }
        public bool ReplaceAll { get; set; }

        public SubFlags(RegexOptions options, int firstMatch, bool replaceAll)
        {
            Options = options;
            FirstMatch = firstMatch;
            ReplaceAll = replaceAll;
        }
    }
}
