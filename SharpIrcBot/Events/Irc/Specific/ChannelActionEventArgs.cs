using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class ChannelActionEventArgs : IChannelMessageEventArgs
    {
        [NotNull] protected ActionEventArgs ActionArgs { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string SenderNickname => ActionArgs.Data.Nick;
        public string Channel => ActionArgs.Data.Channel;
        public string Message => ActionArgs.ActionMessage;

        public ChannelActionEventArgs(ActionEventArgs actionArgs)
        {
            ActionArgs = actionArgs;
            RawMessage = new RawMessageEventArgs(ActionArgs.Data);
        }
    }
}
