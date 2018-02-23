using System;
using System.IO;
using Antlr4.Runtime;

namespace SharpIrcBot.Plugins.Calc
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
            throw new SimplificationException("Syntax error.", null);
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            throw new SimplificationException("Syntax error.", null);
        }
    }
}
