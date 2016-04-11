using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface INickChangeEventArgs
    {
        [NotNull] IRawMessageEventArgs RawMessage { get; }
        [NotNull] string OldNickname { get; }
        [NotNull] string NewNickname { get; }
        [NotNull] string Username { get; }
        [NotNull] string Host { get; }
    }
}
