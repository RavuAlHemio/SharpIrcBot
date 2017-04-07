using JetBrains.Annotations;

namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public class GenericReplacementCommand
    {
        [NotNull]
        public string Command { get; set; }

        [NotNull]
        public string OldString { get; set; }

        [NotNull]
        public string NewString { get; set; }

        [CanBeNull]
        public string Flags { get; set; }

        public GenericReplacementCommand([NotNull] string command, [NotNull] string oldString,
                [NotNull] string newString, [CanBeNull] string flags)
        {
            Command = command;
            OldString = oldString;
            NewString = newString;
            Flags = flags;
        }
    }
}
