using System;
using JetBrains.Annotations;

namespace SharpIrcBot
{
    public interface ITimerTrigger
    {
        void Register(DateTimeOffset when, [NotNull] Action what);
        bool Unregister(DateTimeOffset when, [NotNull] Action what);
    }
}
