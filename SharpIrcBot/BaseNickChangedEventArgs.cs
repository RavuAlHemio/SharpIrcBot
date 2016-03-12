using System;
using JetBrains.Annotations;

namespace SharpIrcBot
{
    public class BaseNickChangedEventArgs : EventArgs
    {
        [NotNull]
        public string OldBaseNick { get; private set; }
        [NotNull]
        public string NewBaseNick { get; private set; }

        public BaseNickChangedEventArgs(string oldBaseNick, string newBaseNick)
            : base()
        {
            OldBaseNick = oldBaseNick;
            NewBaseNick = newBaseNick;
        }
    }
}
