using System;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    [AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class EventValueAttribute : Attribute
    {
        public string ValueName { get; }

        public EventValueAttribute(string valueName)
        {
            ValueName = valueName;
        }
    }
}
