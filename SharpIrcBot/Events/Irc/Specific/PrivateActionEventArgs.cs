using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class PrivateActionEventArgs : IPrivateMessageEventArgs
    {
        [NotNull] protected ActionEventArgs ActionArgs { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string SenderNickname => ActionArgs.Data.Nick;
        public string Message => ActionArgs.ActionMessage;

        public PrivateActionEventArgs(ActionEventArgs actionArgs)
        {
            ActionArgs = actionArgs;
            RawMessage = new RawMessageEventArgs(ActionArgs.Data);
        }
    }
}
