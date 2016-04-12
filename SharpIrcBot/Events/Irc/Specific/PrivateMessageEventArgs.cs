using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class PrivateMessageEventArgs : IPrivateMessageEventArgs
    {
        [NotNull] protected IrcMessageData IrcData { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string SenderNickname => IrcData.Nick;
        public string Message => IrcData.Message;

        public PrivateMessageEventArgs([NotNull] IrcMessageData ircData)
        {
            IrcData = ircData;
            RawMessage = new RawMessageEventArgs(IrcData);
        }
    }
}
