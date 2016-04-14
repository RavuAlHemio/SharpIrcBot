using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface IUserJoinedChannelEventArgs
    {
        [NotNull] IRawMessageEventArgs RawMessage { get; }
        [NotNull] string Nickname { get; }
        [NotNull] string Username { get; }
        [NotNull] string Host { get; }
        [NotNull] string Channel { get; }
    }
}
