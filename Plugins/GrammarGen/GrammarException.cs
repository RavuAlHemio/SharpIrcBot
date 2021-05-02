using System;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class GrammarException : GrammarGenException
    {
        public GrammarException(string message, Exception innerEx = null)
            : base(message, innerEx)
        {
        }
    }
}
