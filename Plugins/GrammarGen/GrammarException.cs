using System;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class GrammarException : Exception
    {
        public GrammarException(string message, Exception innerEx = null)
            : base(message, innerEx)
        {
        }
    }
}
