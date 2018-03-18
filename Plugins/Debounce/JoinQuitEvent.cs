using System;

namespace SharpIrcBot.Plugins.Debounce
{
    public class JoinQuitEvent
    {
        public string Channel { get; }
        public string Nickname { get; }
        public DateTimeOffset Timestamp { get; }

        public JoinQuitEvent(string channel, string nickname, DateTimeOffset timestamp)
        {
            Channel = channel;
            Nickname = nickname;
            Timestamp = timestamp;
        }
    }
}
