namespace SharpIrcBot.Tests.TestPlumbing.Events.Logging
{
    public enum MessageType
    {
        Unknown = 0,
        Message = 1,
        Action = 2,
        Notice = 3,
        CTCPRequest = 4,
        CTCPResponse = 5,
    }
}
