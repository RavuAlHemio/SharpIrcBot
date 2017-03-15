namespace SharpIrcBot.TestPlumbing.Events.Logging
{
    public class TestMessage : ITestIrcEvent
    {
        public MessageType Type { get; set; }
        public string Target { get; set; }
        public string Body { get; set; }
    }
}
