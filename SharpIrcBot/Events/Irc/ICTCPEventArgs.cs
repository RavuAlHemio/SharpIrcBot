using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface ICTCPEventArgs : IUserMessageEventArgs
    {
        [NotNull] string CTCPCommand { get; }
        [CanBeNull] string CTCPParameter { get; }
    }
}
