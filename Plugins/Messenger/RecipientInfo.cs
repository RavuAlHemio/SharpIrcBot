using System.Collections.Generic;
using JetBrains.Annotations;

namespace Messenger
{
    public class RecipientInfo
    {
        public string RecipientNick { get; }
        public string Recipient { get; }
        public string LowerRecipient { get; }

        public RecipientInfo([NotNull] string recipientNick, [CanBeNull] string recipient)
        {
            RecipientNick = recipientNick;
            Recipient = recipient ?? recipientNick;
            LowerRecipient = Recipient.ToLowerInvariant();
        }

        public class LowerRecipientComparer : EqualityComparer<RecipientInfo>
        {
            public override bool Equals(RecipientInfo x, RecipientInfo y)
            {
                return x.LowerRecipient.Equals(y.LowerRecipient);
            }

            public override int GetHashCode(RecipientInfo obj)
            {
                return obj.LowerRecipient.GetHashCode();
            }
        }
    }
}
