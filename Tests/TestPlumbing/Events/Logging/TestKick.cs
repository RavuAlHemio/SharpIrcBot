namespace SharpIrcBot.Tests.TestPlumbing.Events.Logging
{
    public class TestKick : ITestIrcEvent
    {
        public string Nickname { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
    }
}
