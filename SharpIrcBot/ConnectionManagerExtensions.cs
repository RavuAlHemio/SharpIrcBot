using JetBrains.Annotations;

namespace SharpIrcBot
{
    public static class ConnectionManagerExtensions
    {
        public static void SendChannelMessageFormat([NotNull] this IConnectionManager connMgr, [NotNull] string channel, [NotNull] string format, [NotNull, ItemCanBeNull] params object[] args)
        {
            connMgr.SendChannelMessage(channel, string.Format(format, args));
        }

        public static void SendChannelActionFormat([NotNull] this IConnectionManager connMgr, [NotNull] string channel, [NotNull] string format, [NotNull, ItemCanBeNull] params object[] args)
        {
            connMgr.SendChannelAction(channel, string.Format(format, args));
        }

        public static void SendChannelNoticeFormat([NotNull] this IConnectionManager connMgr, [NotNull] string channel, [NotNull] string format, [NotNull, ItemCanBeNull] params object[] args)
        {
            connMgr.SendChannelNotice(channel, string.Format(format, args));
        }

        public static void SendQueryMessageFormat([NotNull] this IConnectionManager connMgr, [NotNull] string nick, [NotNull] string format, [NotNull, ItemCanBeNull] params object[] args)
        {
            connMgr.SendQueryMessage(nick, string.Format(format, args));
        }

        public static void SendQueryActionFormat([NotNull] this IConnectionManager connMgr, [NotNull] string nick, [NotNull] string format, [NotNull, ItemCanBeNull] params object[] args)
        {
            connMgr.SendQueryAction(nick, string.Format(format, args));
        }

        public static void SendQueryNoticeFormat([NotNull] this IConnectionManager connMgr, [NotNull] string nick, [NotNull] string format, [NotNull, ItemCanBeNull] params object[] args)
        {
            connMgr.SendQueryNotice(nick, string.Format(format, args));
        }
    }
}
