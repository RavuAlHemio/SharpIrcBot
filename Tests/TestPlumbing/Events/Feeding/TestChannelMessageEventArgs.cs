using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Tests.TestPlumbing.Events.Feeding
{
    public class TestChannelMessageEventArgs : IChannelMessageEventArgs
    {
        public string SenderNickname { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public IRawMessageEventArgs RawMessage => null;
    }
}
