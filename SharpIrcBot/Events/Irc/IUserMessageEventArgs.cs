using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface IUserMessageEventArgs
    {
        [NotNull] IRawMessageEventArgs RawMessage { get; }
        [NotNull] string SenderNickname { get; }
        [NotNull] string Message { get; }
    }
}
