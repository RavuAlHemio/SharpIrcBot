using System.Collections.Generic;
using JetBrains.Annotations;

namespace SharpIrcBot.Plugins.Messenger
{
    public class RecipientInfo
    {
        [NotNull] public string RecipientNick { get; }
        [NotNull] public string LowerRecipientNick { get; }
        [CanBeNull] public string RecipientUser { get; }
        [CanBeNull] public string LowerRecipientUser { get; }
        public bool ExactNickname { get; }

        [NotNull] public string Recipient => ExactNickname ? RecipientNick : RecipientUser;
        [NotNull] public string LowerRecipient => ExactNickname ? LowerRecipientNick : LowerRecipientUser;

        public RecipientInfo([NotNull] string recipientNick, [CanBeNull] string recipientUser, bool exactNickname)
        {
            RecipientNick = recipientNick;
            RecipientUser = recipientUser ?? recipientNick;

            LowerRecipientNick = RecipientNick.ToLowerInvariant();
            LowerRecipientUser = RecipientUser.ToLowerInvariant();

            ExactNickname = exactNickname;
        }

        public class LowerRecipientComparer : EqualityComparer<RecipientInfo>
        {
            private static LowerRecipientComparer _instance = null;

            public static LowerRecipientComparer Instance
                => _instance ?? (_instance = new LowerRecipientComparer());

            protected LowerRecipientComparer()
            {
            }

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
