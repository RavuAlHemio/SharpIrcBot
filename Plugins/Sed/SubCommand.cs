namespace SharpIrcBot.Plugins.Sed
{
    public class SubCommand
    {
        public string PatternSed { get; set; }
        public string ReplacementSed { get; set; }
        public string Flags { get; set; }

        public SubCommand(string patternSed, string replacementSed, string flags)
        {
            PatternSed = patternSed;
            ReplacementSed = replacementSed;
            Flags = flags;
        }
    }
}
