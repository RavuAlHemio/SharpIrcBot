using System;

namespace Messenger.ORM
{
    public class ReplayableMessage : IMessage
    {
        public long ID { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string SenderOriginal { get; set; }

        public string RecipientLowercase { get; set; }

        public string Body { get; set; }

        public ReplayableMessage()
        {
        }

        public ReplayableMessage(IMessage other)
        {
            MessageUtils.TransferMessage(other, this);
        }
    }
}
