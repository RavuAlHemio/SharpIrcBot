using System.Collections.Generic;
using JetBrains.Annotations;

namespace Messenger
{
    public class RecipientInfo
    {
        [NotNull] public string RecipientNick { get; }
        [NotNull] public string Recipient { get; }
        [NotNull] public string LowerRecipient { get; }

        public RecipientInfo([NotNull] string recipientNick, [CanBeNull] string recipient)
        {
            RecipientNick = recipientNick;
            Recipient = recipient ?? recipientNick;
            LowerRecipient = Recipient.ToLowerInvariant();
        }

        public class LowerRecipientComparer : EqualityComparer<RecipientInfo>
        {
            public override bool Equals([NotNull] RecipientInfo x, [NotNull] RecipientInfo y)
            {
                return x.LowerRecipient.Equals(y.LowerRecipient);
            }

            public override int GetHashCode(RecipientInfo obj)
            {
                return obj?.LowerRecipient.GetHashCode() ?? 0;
            }
        }
    }
}
