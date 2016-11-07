namespace Messenger.ORM
{
    public class Message : IMessage
    {
        public long ID { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string SenderOriginal { get; set; }

        public string RecipientLowercase { get; set; }

        public string Body { get; set; }

        public Message()
        {
        }

        public Message(IMessage other)
        {
            MessageUtils.TransferMessage(other, this);
        }
    }
}
