using System;
using System.IO;
using Antlr4.Runtime;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class PanicErrorListener : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
    {
        private static Lazy<PanicErrorListener> _instance =
            new Lazy<PanicErrorListener>(() => new PanicErrorListener());
        public static PanicErrorListener Instance => _instance.Value;

        protected PanicErrorListener()
        {
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            throw new GrammarException($"Lexer error on line {line} at character {charPositionInLine}: {msg}");
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            throw new GrammarException($"Parser error on line {line} at character {charPositionInLine}: {msg}");
        }
    }
}
