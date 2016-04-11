using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface IUserLeftChannelEventArgs
    {
        [NotNull] IRawMessageEventArgs RawMessage { get; }
        [NotNull] string User { get; }
        [NotNull] string Channel { get; }
        [CanBeNull] string Message { get; }
    }
}
