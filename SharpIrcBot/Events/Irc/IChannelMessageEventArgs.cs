using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface IChannelMessageEventArgs : IUserMessageEventArgs
    {
        [NotNull] string Channel { get; }
    }
}
