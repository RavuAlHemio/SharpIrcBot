using System;

namespace Messenger.ORM
{
    public interface IMessage
    {
        long ID { get; set; }

        DateTime Timestamp { get; set; }

        string SenderOriginal { get; set; }

        string RecipientLowercase { get; set; }

        string Body { get; set; }
    }
}
