using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class UserLeftChannelEventArgs : IUserLeftChannelEventArgs
    {
        [NotNull] protected PartEventArgs PartArgs { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string User => PartArgs.Who;
        public string Channel => PartArgs.Channel;
        public string Message => PartArgs.PartMessage;

        public UserLeftChannelEventArgs(PartEventArgs partArgs)
        {
            PartArgs = partArgs;
            RawMessage = new RawMessageEventArgs(PartArgs.Data);
        }
    }
}
