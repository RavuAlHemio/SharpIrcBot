using System;

namespace Messenger.ORM
{
    public class PrivateMessage : IMessage
    {
        public long ID { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string SenderOriginal { get; set; }

        public string RecipientLowercase { get; set; }

        public string Body { get; set; }

        public PrivateMessage()
        {
        }

        public PrivateMessage(IMessage other)
        {
            MessageUtils.TransferMessage(other, this);
        }
    }
}
