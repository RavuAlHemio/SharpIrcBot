using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface IUserQuitServerEventArgs
    {
        [NotNull] IRawMessageEventArgs RawMessage { get; }
        [NotNull] string User { get; }
        [CanBeNull] string Message { get; }
    }
}
