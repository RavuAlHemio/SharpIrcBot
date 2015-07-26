using System;

namespace UnoBot.RuntimeTweaking
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NotTweakableAtRuntimeAttribute : Attribute
    {
    }
}
