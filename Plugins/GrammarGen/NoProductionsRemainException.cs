using System;
using SharpIrcBot.Plugins.GrammarGen.AST;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class NoProductionsRemainException : Exception
    {
        public Production Production { get; }

        public NoProductionsRemainException(Production production, Exception innerEx = null)
            : base($"no productions available after processing conditions at {production.ToString()}", innerEx)
        {
            Production = production;
        }
    }
}
