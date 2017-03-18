using System;

namespace SharpIrcBot.Plugins.UnoBot.RuntimeTweaking
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NotTweakableAtRuntimeAttribute : Attribute
    {
    }
}
