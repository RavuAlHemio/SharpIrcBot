using System;
using System.IO;
using SharpIrcBot.Plugins.GrammarGen;
using SharpIrcBot.Util;

namespace GrammarGenTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: GrammarGenTest GRAMMARFILE STARTRULE");
                Environment.ExitCode = 1;
                return;
            }

            string grammarDef;
            using (var grammarFile = new StreamReader(args[0], StringUtil.Utf8NoBom))
            {
                grammarDef = grammarFile.ReadToEnd();
            }

            string startRule = args[1];

            var grammar = new Grammar(grammarDef, startRule);
            foreach (var r in grammar.Rules.Rules)
            {
                Console.WriteLine(r);
            }
            Console.WriteLine();

            Console.WriteLine(grammar.Generate());
        }
    }
}
