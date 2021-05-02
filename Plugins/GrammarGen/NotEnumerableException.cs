using System;
using SharpIrcBot.Plugins.GrammarGen.AST;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class NotEnumerableException : GrammarGenException
    {
        public Production Production { get; }

        public NotEnumerableException(Production production, Exception innerEx = null)
            : base($"this grammar is not enumerable due to {production.ToString()}", innerEx)
        {
            Production = production;
        }
    }
}
