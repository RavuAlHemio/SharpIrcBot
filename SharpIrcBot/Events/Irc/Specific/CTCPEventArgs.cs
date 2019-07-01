using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class CTCPEventArgs : ICTCPEventArgs
    {
        [NotNull] protected CtcpEventArgs CTCPArgs { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string SenderNickname => CTCPArgs.Data.Nick;
        public string Message => CTCPArgs.Data.Message;

        [NotNull] public string CTCPCommand => CTCPArgs.CtcpCommand;
        [CanBeNull] public string CTCPParameter =>
            (CTCPArgs.CtcpParameter.Length > 0)
            ? CTCPArgs.CtcpParameter
            : null;

        public CTCPEventArgs([NotNull] CtcpEventArgs ctcpArgs)
        {
            CTCPArgs = ctcpArgs;
            RawMessage = new RawMessageEventArgs(ctcpArgs.Data);
        }
    }
}
