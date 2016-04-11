using System.Collections.Generic;
using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface IRawMessageEventArgs
    {
        [CanBeNull] int? ReplyCode { get; }
        [NotNull] string RawMessageString { get; }
        [NotNull, ItemNotNull] IReadOnlyList<string> RawMessageParts { get; }
    }
}
