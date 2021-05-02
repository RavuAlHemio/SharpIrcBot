using System;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class GrammarGenException : Exception
    {
        public GrammarGenException(string message, Exception innerEx = null)
            : base(message, innerEx)
        {
        }
    }
}
