using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc
{
    public interface IUserInvitedToChannelEventArgs
    {
        [NotNull] IRawMessageEventArgs RawMessage { get; }
        [NotNull] string Invitee { get; }
        [NotNull] string Channel { get; }
    }
}
