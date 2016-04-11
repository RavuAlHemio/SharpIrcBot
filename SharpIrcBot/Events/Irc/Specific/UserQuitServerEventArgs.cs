using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class UserQuitServerEventArgs : IUserQuitServerEventArgs
    {
        [NotNull] protected QuitEventArgs QuitArgs { get; }
        public IRawMessageEventArgs RawMessage { get; }

        public string User => QuitArgs.Who;
        public string Message => QuitArgs.QuitMessage;

        public UserQuitServerEventArgs(QuitEventArgs quitArgs)
        {
            QuitArgs = quitArgs;
            RawMessage = new RawMessageEventArgs(QuitArgs.Data);
        }
    }
}
