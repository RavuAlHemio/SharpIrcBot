using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class UserJoinedChannelEventArgs : IUserJoinedChannelEventArgs
    {
        [NotNull] protected JoinEventArgs JoinArgs { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string Nickname => JoinArgs.Who;
        public string Host => JoinArgs.Data.Host;
        public string Username => JoinArgs.Data.Ident;
        public string Channel => JoinArgs.Channel;

        public UserJoinedChannelEventArgs([NotNull] JoinEventArgs joinArgs)
        {
            JoinArgs = joinArgs;
            RawMessage = new RawMessageEventArgs(JoinArgs.Data);
        }
    }
}
