using System;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    [AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class EventAttribute : Attribute
    {
        public string EventName { get; }

        public EventAttribute(string eventName)
        {
            EventName = eventName;
        }
    }
}
