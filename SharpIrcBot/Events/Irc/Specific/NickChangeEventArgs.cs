using JetBrains.Annotations;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class NickChangeEventArgs : INickChangeEventArgs
    {
        [NotNull] protected Meebey.SmartIrc4net.NickChangeEventArgs NickChangeArgs { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string OldNickname => NickChangeArgs.OldNickname;
        public string NewNickname => NickChangeArgs.NewNickname;
        public string Host => NickChangeArgs.Data.Host;
        public string Username => NickChangeArgs.Data.Ident;

        public NickChangeEventArgs([NotNull] Meebey.SmartIrc4net.NickChangeEventArgs nickChangeArgs)
        {
            NickChangeArgs = nickChangeArgs;
            RawMessage = new RawMessageEventArgs(NickChangeArgs.Data);
        }
    }
}
