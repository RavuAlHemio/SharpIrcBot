using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class ChannelMessageEventArgs : IChannelMessageEventArgs
    {
        [NotNull] protected IrcMessageData IrcData { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string SenderNickname => IrcData.Nick;
        public string Channel => IrcData.Channel;
        public string Message => IrcData.Message;

        public ChannelMessageEventArgs(IrcMessageData ircData)
        {
            IrcData = ircData;
            RawMessage = new RawMessageEventArgs(IrcData);
        }
    }
}
