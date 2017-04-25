using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Commands
{
    public class CommandMatch
    {
        public Command Command { get; }
        public string CommandName { get; }
        public ImmutableList<KeyValuePair<string, object>> Options { get; }
        public ImmutableList<object> Arguments { get; }
        public MessageFlags MessageFlags { get; }

        public CommandMatch(Command command, string commandName, ImmutableList<KeyValuePair<string, object>> options,
                ImmutableList<object> arguments, MessageFlags messageFlags)
        {
            Command = command;
            CommandName = commandName;
            Options = options;
            Arguments = arguments;
            MessageFlags = messageFlags;
        }
    }
}
