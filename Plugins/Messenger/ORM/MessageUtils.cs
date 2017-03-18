namespace SharpIrcBot.Plugins.Messenger.ORM
{
    public static class MessageUtils
    {
        public static void TransferMessage(IMessage fromMessage, IMessage toMessage)
        {
            toMessage.ID = fromMessage.ID;
            toMessage.Timestamp = fromMessage.Timestamp;
            toMessage.SenderOriginal = fromMessage.SenderOriginal;
            toMessage.RecipientLowercase = fromMessage.RecipientLowercase;
            toMessage.Body = fromMessage.Body;
        }
    }
}
