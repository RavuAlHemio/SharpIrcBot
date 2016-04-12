using System;
using JetBrains.Annotations;

namespace SharpIrcBot.Events
{
    public class BaseNickChangedEventArgs : EventArgs
    {
        [NotNull]
        public string OldBaseNick { get; private set; }
        [NotNull]
        public string NewBaseNick { get; private set; }

        public BaseNickChangedEventArgs([NotNull] string oldBaseNick, [NotNull] string newBaseNick)
            : base()
        {
            OldBaseNick = oldBaseNick;
            NewBaseNick = newBaseNick;
        }
    }
}
