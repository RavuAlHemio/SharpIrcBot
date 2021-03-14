using System;
using System.Collections.Immutable;
using System.IO;
using SharpIrcBot.Plugins.GrammarGen;
using SharpIrcBot.Plugins.GrammarGen.AST;
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

            // generate the built-in rules
            var builder = ImmutableDictionary.CreateBuilder<string, Rule>();
            builder["__IRC_channel"] = new Rule("__IRC_channel", new StrProduction("#test"));
            builder["__IRC_nick"] = new Rule("__IRC_nick", new StrProduction("SampleNick"));
            builder["__IRC_chosen_nick"] = new Rule("__IRC_chosen_nick", new StrProduction("SampleNick"));
            var builtInRules = new Rulebook(builder.ToImmutable());

            var grammar = new Grammar(grammarDef, startRule, builtInRules);
            foreach (var r in grammar.Rules.Rules)
            {
                Console.WriteLine(r);
            }
            Console.WriteLine();

            Console.WriteLine(grammar.Generate());
        }
    }
}
