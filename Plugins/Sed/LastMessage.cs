namespace SharpIrcBot.Plugins.Sed
{
    public class LastMessage
    {
        public LastMessageType Type { get; }
        public string AuthorNickname { get; }
        public string Body { get; }

        public LastMessage(LastMessageType type, string nick, string body)
        {
            Type = type;
            AuthorNickname = nick;
            Body = body;
        }
    }
}
