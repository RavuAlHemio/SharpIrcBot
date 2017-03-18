using System;

namespace SharpIrcBot.Plugins.Messenger.ORM
{
    public interface IMessage
    {
        long ID { get; set; }

        DateTimeOffset Timestamp { get; set; }

        string SenderOriginal { get; set; }

        string RecipientLowercase { get; set; }

        string Body { get; set; }
    }
}
