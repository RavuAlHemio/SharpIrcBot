using System.Collections.Generic;
using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface INameListEventArgs
    {
        [NotNull] IRawMessageEventArgs RawMessage { get; }
        [NotNull, ItemNotNull] IReadOnlyList<string> Nicknames { get; }
    }
}
