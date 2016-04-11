using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class UserInvitedToChannelEventArgs : IUserInvitedToChannelEventArgs
    {
        [NotNull] protected InviteEventArgs InviteArgs { get; }
		public IRawMessageEventArgs RawMessage { get; }

        public string Invitee => InviteArgs.Who;
        public string Channel => InviteArgs.Channel;

        public UserInvitedToChannelEventArgs(InviteEventArgs inviteArgs)
        {
            InviteArgs = inviteArgs;
            RawMessage = new RawMessageEventArgs(InviteArgs.Data);
        }
    }
}
