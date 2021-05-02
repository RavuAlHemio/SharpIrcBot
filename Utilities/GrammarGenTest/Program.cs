﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using SharpIrcBot.Plugins.GrammarGen;
using SharpIrcBot.Plugins.GrammarGen.AST;
using SharpIrcBot.Util;

namespace GrammarGenTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: GrammarGenTest GRAMMARFILE [GENCOUNT [CONDITION...]]");
                Console.Error.WriteLine("  pass -1 as GENCOUNT to get the bounds on the number of variants");
                Console.Error.WriteLine("  pass any another negative as GENCOUNT to enumerate all variants");
                Environment.ExitCode = 1;
                return;
            }

            string grammarDef;
            using (var grammarFile = new StreamReader(args[0], StringUtil.Utf8NoBom))
            {
                grammarDef = grammarFile.ReadToEnd();
            }

            string startRule = Path.GetFileNameWithoutExtension(args[0]);
            int genCount = 1;
            if (args.Length > 1)
            {
                genCount = int.Parse(args[1]);
            }

            ImmutableDictionary<string, object> parameters = args
                .Skip(2)
                .Select(p => KeyValuePair.Create(p, (object)true))
                .ToImmutableDictionary();

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

            if (genCount == -1)
            {
                // output number of combinations
                var variantBounds = grammar.CountVariantBounds(parameters);
                string upperBound = variantBounds.Upper.HasValue
                    ? $"{variantBounds.Upper.Value}"
                    : "infinity"
                ;
                Console.WriteLine($"from {variantBounds.Lower} to {upperBound}");
            }
            else if (genCount < 0)
            {
                long totalCount = 0;
                foreach (string generation in grammar.GenerateAll(parameters))
                {
                    totalCount++;
                    Console.WriteLine($"{totalCount}: {generation}");
                }
                Console.WriteLine();
                Console.WriteLine($"total entries: {totalCount}");
            }
            else
            {
                for (int i = 0; i < genCount; i++)
                {
                    Console.WriteLine(grammar.Generate(
                        rng: null,
                        parameters: parameters
                    ));
                }
            }
        }
    }
}
