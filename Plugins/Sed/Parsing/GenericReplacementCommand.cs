using System.Diagnostics.Contracts;

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
            Contract.Requires(command != null);
            Contract.Requires(oldString != null);
            Contract.Requires(newString != null);

            Command = command;
            OldString = oldString;
            NewString = newString;
            Flags = flags;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Command != null);
            Contract.Invariant(OldString != null);
            Contract.Invariant(NewString != null);
        }
    }
}
