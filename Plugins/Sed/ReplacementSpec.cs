using System.Text.RegularExpressions;

namespace SharpIrcBot.Plugins.Sed
{
    public class ReplacementSpec
    {
        public Regex Pattern { get; set; }
        public string Replacement { get; set; }
        public int FirstMatch { get; set; }
        public bool ReplaceAll { get; set; }
    }
}
