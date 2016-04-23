using System.Collections.Generic;
using JetBrains.Annotations;
using Meebey.SmartIrc4net;

namespace SharpIrcBot.Events.Irc.Specific
{
    internal class RawMessageEventArgs : IRawMessageEventArgs
    {
        [NotNull] protected IrcMessageData IrcData { get; }

        public int? ReplyCode => (IrcData.ReplyCode == 0) ? (int?) null : (int) IrcData.ReplyCode;
        public string RawMessageString => IrcData.RawMessage;
        public IReadOnlyList<string> RawMessageParts => IrcData.RawMessageArray;

        public RawMessageEventArgs([NotNull] IrcMessageData ircData)
        {
            IrcData = ircData;
        }
    }
}
