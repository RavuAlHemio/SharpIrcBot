namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public class GenericReplacementCommand
    {
        public string Command { get; set; }
        public string OldString { get; set; }
        public string NewString { get; set; }
        public string Flags { get; set; }

        public GenericReplacementCommand(string command, string oldString, string newString, string flags)
        {
            Command = command;
            OldString = oldString;
            NewString = newString;
            Flags = flags;
        }
    }
}
