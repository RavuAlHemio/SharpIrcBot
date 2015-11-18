using System;

namespace SharpIrcBot
{
    public class OutgoingMessageEventArgs : EventArgs
    {
        public string Target { get; private set; }

        public string OutgoingMessage { get; private set; }

        public OutgoingMessageEventArgs(string target, string outgoingMessage)
        {
            Target = target;
            OutgoingMessage = outgoingMessage;
        }
    }
}
