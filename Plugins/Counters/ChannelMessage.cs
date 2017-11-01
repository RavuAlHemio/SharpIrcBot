namespace SharpIrcBot.Plugins.Counters
{
    public class ChannelMessage
    {
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string Body { get; set; }
        public bool Counted { get; set; }
    }
}
