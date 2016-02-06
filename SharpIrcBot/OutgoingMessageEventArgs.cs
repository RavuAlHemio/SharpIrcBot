using System;
using JetBrains.Annotations;

namespace SharpIrcBot
{
    public class OutgoingMessageEventArgs : EventArgs
    {
        [NotNull]
        public string Target { get; private set; }
        [NotNull]
        public string OutgoingMessage { get; private set; }

        public OutgoingMessageEventArgs([NotNull] string target, [NotNull] string outgoingMessage)
        {
            Target = target;
            OutgoingMessage = outgoingMessage;
        }
    }
}
