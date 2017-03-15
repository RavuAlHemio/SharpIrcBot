namespace SharpIrcBot.TestPlumbing.Events.Logging
{
    public class TestRawCommand : ITestIrcEvent
    {
        public string Command { get; set; }
    }
}
