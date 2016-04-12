using System.Collections.Generic;
using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class NameListEventArgs : INameListEventArgs
    {
        [NotNull] protected NamesEventArgs NamesArgs { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public IReadOnlyList<string> Nicknames => NamesArgs.UserList;

        public NameListEventArgs([NotNull] NamesEventArgs namesArgs)
        {
            NamesArgs = namesArgs;
            RawMessage = new RawMessageEventArgs(NamesArgs.Data);
        }
    }
}
