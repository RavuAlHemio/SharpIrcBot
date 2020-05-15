//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.8
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from CalcLang.g4 by ANTLR 4.8

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace SharpIrcBot.Plugins.Calc.Language {
using System;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.8")]
[System.CLSCompliant(false)]
public partial class CalcLangLexer : Lexer {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, T__6=7, T__7=8, T__8=9, 
		T__9=10, T__10=11, T__11=12, T__12=13, T__13=14, Whitespaces=15, Decimal=16, 
		Identifier=17, Integer=18, Integer10=19, Integer16=20, Integer8=21, Integer2=22;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE"
	};

	public static readonly string[] ruleNames = {
		"T__0", "T__1", "T__2", "T__3", "T__4", "T__5", "T__6", "T__7", "T__8", 
		"T__9", "T__10", "T__11", "T__12", "T__13", "Whitespaces", "Decimal", 
		"Identifier", "Integer", "Integer10", "Integer16", "Integer8", "Integer2", 
		"Whitespace"
	};


	public CalcLangLexer(ICharStream input)
	: this(input, Console.Out, Console.Error) { }

	public CalcLangLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
	: base(input, output, errorOutput)
	{
		Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	private static readonly string[] _LiteralNames = {
		null, "'('", "')'", "'!'", "'-'", "'**'", "'*'", "'//'", "'/'", "'%'", 
		"'+'", "'&'", "'^'", "'|'", "','"
	};
	private static readonly string[] _SymbolicNames = {
		null, null, null, null, null, null, null, null, null, null, null, null, 
		null, null, null, "Whitespaces", "Decimal", "Identifier", "Integer", "Integer10", 
		"Integer16", "Integer8", "Integer2"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "CalcLang.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string[] ChannelNames { get { return channelNames; } }

	public override string[] ModeNames { get { return modeNames; } }

	public override string SerializedAtn { get { return new string(_serializedATN); } }

	static CalcLangLexer() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}
	private static char[] _serializedATN = {
		'\x3', '\x608B', '\xA72A', '\x8133', '\xB9ED', '\x417C', '\x3BE7', '\x7786', 
		'\x5964', '\x2', '\x18', '\x8D', '\b', '\x1', '\x4', '\x2', '\t', '\x2', 
		'\x4', '\x3', '\t', '\x3', '\x4', '\x4', '\t', '\x4', '\x4', '\x5', '\t', 
		'\x5', '\x4', '\x6', '\t', '\x6', '\x4', '\a', '\t', '\a', '\x4', '\b', 
		'\t', '\b', '\x4', '\t', '\t', '\t', '\x4', '\n', '\t', '\n', '\x4', '\v', 
		'\t', '\v', '\x4', '\f', '\t', '\f', '\x4', '\r', '\t', '\r', '\x4', '\xE', 
		'\t', '\xE', '\x4', '\xF', '\t', '\xF', '\x4', '\x10', '\t', '\x10', '\x4', 
		'\x11', '\t', '\x11', '\x4', '\x12', '\t', '\x12', '\x4', '\x13', '\t', 
		'\x13', '\x4', '\x14', '\t', '\x14', '\x4', '\x15', '\t', '\x15', '\x4', 
		'\x16', '\t', '\x16', '\x4', '\x17', '\t', '\x17', '\x4', '\x18', '\t', 
		'\x18', '\x3', '\x2', '\x3', '\x2', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x4', '\x3', '\x4', '\x3', '\x5', '\x3', '\x5', '\x3', '\x6', '\x3', 
		'\x6', '\x3', '\x6', '\x3', '\a', '\x3', '\a', '\x3', '\b', '\x3', '\b', 
		'\x3', '\b', '\x3', '\t', '\x3', '\t', '\x3', '\n', '\x3', '\n', '\x3', 
		'\v', '\x3', '\v', '\x3', '\f', '\x3', '\f', '\x3', '\r', '\x3', '\r', 
		'\x3', '\xE', '\x3', '\xE', '\x3', '\xF', '\x3', '\xF', '\x3', '\x10', 
		'\x6', '\x10', 'Q', '\n', '\x10', '\r', '\x10', '\xE', '\x10', 'R', '\x3', 
		'\x10', '\x3', '\x10', '\x3', '\x11', '\x6', '\x11', 'X', '\n', '\x11', 
		'\r', '\x11', '\xE', '\x11', 'Y', '\x3', '\x11', '\x3', '\x11', '\x6', 
		'\x11', '^', '\n', '\x11', '\r', '\x11', '\xE', '\x11', '_', '\x3', '\x12', 
		'\x3', '\x12', '\a', '\x12', '\x64', '\n', '\x12', '\f', '\x12', '\xE', 
		'\x12', 'g', '\v', '\x12', '\x3', '\x13', '\x3', '\x13', '\x3', '\x13', 
		'\x3', '\x13', '\x5', '\x13', 'm', '\n', '\x13', '\x3', '\x14', '\x6', 
		'\x14', 'p', '\n', '\x14', '\r', '\x14', '\xE', '\x14', 'q', '\x3', '\x15', 
		'\x3', '\x15', '\x3', '\x15', '\x3', '\x15', '\x6', '\x15', 'x', '\n', 
		'\x15', '\r', '\x15', '\xE', '\x15', 'y', '\x3', '\x16', '\x3', '\x16', 
		'\x3', '\x16', '\x3', '\x16', '\x6', '\x16', '\x80', '\n', '\x16', '\r', 
		'\x16', '\xE', '\x16', '\x81', '\x3', '\x17', '\x3', '\x17', '\x3', '\x17', 
		'\x3', '\x17', '\x6', '\x17', '\x88', '\n', '\x17', '\r', '\x17', '\xE', 
		'\x17', '\x89', '\x3', '\x18', '\x3', '\x18', '\x2', '\x2', '\x19', '\x3', 
		'\x3', '\x5', '\x4', '\a', '\x5', '\t', '\x6', '\v', '\a', '\r', '\b', 
		'\xF', '\t', '\x11', '\n', '\x13', '\v', '\x15', '\f', '\x17', '\r', '\x19', 
		'\xE', '\x1B', '\xF', '\x1D', '\x10', '\x1F', '\x11', '!', '\x12', '#', 
		'\x13', '%', '\x14', '\'', '\x15', ')', '\x16', '+', '\x17', '-', '\x18', 
		'/', '\x2', '\x3', '\x2', '\n', '\x3', '\x2', '\x32', ';', '\x4', '\x2', 
		'\x43', '\\', '\x63', '|', '\x6', '\x2', '\x32', ';', '\x43', '\\', '\x61', 
		'\x61', '\x63', '|', '\x4', '\x2', '\x32', ';', '\x61', '\x61', '\x6', 
		'\x2', '\x32', ';', '\x43', 'H', '\x61', '\x61', '\x63', 'h', '\x4', '\x2', 
		'\x32', '\x39', '\x61', '\x61', '\x4', '\x2', '\x32', '\x33', '\x61', 
		'\x61', '\x4', '\x2', '\v', '\xF', '\"', '\"', '\x2', '\x96', '\x2', '\x3', 
		'\x3', '\x2', '\x2', '\x2', '\x2', '\x5', '\x3', '\x2', '\x2', '\x2', 
		'\x2', '\a', '\x3', '\x2', '\x2', '\x2', '\x2', '\t', '\x3', '\x2', '\x2', 
		'\x2', '\x2', '\v', '\x3', '\x2', '\x2', '\x2', '\x2', '\r', '\x3', '\x2', 
		'\x2', '\x2', '\x2', '\xF', '\x3', '\x2', '\x2', '\x2', '\x2', '\x11', 
		'\x3', '\x2', '\x2', '\x2', '\x2', '\x13', '\x3', '\x2', '\x2', '\x2', 
		'\x2', '\x15', '\x3', '\x2', '\x2', '\x2', '\x2', '\x17', '\x3', '\x2', 
		'\x2', '\x2', '\x2', '\x19', '\x3', '\x2', '\x2', '\x2', '\x2', '\x1B', 
		'\x3', '\x2', '\x2', '\x2', '\x2', '\x1D', '\x3', '\x2', '\x2', '\x2', 
		'\x2', '\x1F', '\x3', '\x2', '\x2', '\x2', '\x2', '!', '\x3', '\x2', '\x2', 
		'\x2', '\x2', '#', '\x3', '\x2', '\x2', '\x2', '\x2', '%', '\x3', '\x2', 
		'\x2', '\x2', '\x2', '\'', '\x3', '\x2', '\x2', '\x2', '\x2', ')', '\x3', 
		'\x2', '\x2', '\x2', '\x2', '+', '\x3', '\x2', '\x2', '\x2', '\x2', '-', 
		'\x3', '\x2', '\x2', '\x2', '\x3', '\x31', '\x3', '\x2', '\x2', '\x2', 
		'\x5', '\x33', '\x3', '\x2', '\x2', '\x2', '\a', '\x35', '\x3', '\x2', 
		'\x2', '\x2', '\t', '\x37', '\x3', '\x2', '\x2', '\x2', '\v', '\x39', 
		'\x3', '\x2', '\x2', '\x2', '\r', '<', '\x3', '\x2', '\x2', '\x2', '\xF', 
		'>', '\x3', '\x2', '\x2', '\x2', '\x11', '\x41', '\x3', '\x2', '\x2', 
		'\x2', '\x13', '\x43', '\x3', '\x2', '\x2', '\x2', '\x15', '\x45', '\x3', 
		'\x2', '\x2', '\x2', '\x17', 'G', '\x3', '\x2', '\x2', '\x2', '\x19', 
		'I', '\x3', '\x2', '\x2', '\x2', '\x1B', 'K', '\x3', '\x2', '\x2', '\x2', 
		'\x1D', 'M', '\x3', '\x2', '\x2', '\x2', '\x1F', 'P', '\x3', '\x2', '\x2', 
		'\x2', '!', 'W', '\x3', '\x2', '\x2', '\x2', '#', '\x61', '\x3', '\x2', 
		'\x2', '\x2', '%', 'l', '\x3', '\x2', '\x2', '\x2', '\'', 'o', '\x3', 
		'\x2', '\x2', '\x2', ')', 's', '\x3', '\x2', '\x2', '\x2', '+', '{', '\x3', 
		'\x2', '\x2', '\x2', '-', '\x83', '\x3', '\x2', '\x2', '\x2', '/', '\x8B', 
		'\x3', '\x2', '\x2', '\x2', '\x31', '\x32', '\a', '*', '\x2', '\x2', '\x32', 
		'\x4', '\x3', '\x2', '\x2', '\x2', '\x33', '\x34', '\a', '+', '\x2', '\x2', 
		'\x34', '\x6', '\x3', '\x2', '\x2', '\x2', '\x35', '\x36', '\a', '#', 
		'\x2', '\x2', '\x36', '\b', '\x3', '\x2', '\x2', '\x2', '\x37', '\x38', 
		'\a', '/', '\x2', '\x2', '\x38', '\n', '\x3', '\x2', '\x2', '\x2', '\x39', 
		':', '\a', ',', '\x2', '\x2', ':', ';', '\a', ',', '\x2', '\x2', ';', 
		'\f', '\x3', '\x2', '\x2', '\x2', '<', '=', '\a', ',', '\x2', '\x2', '=', 
		'\xE', '\x3', '\x2', '\x2', '\x2', '>', '?', '\a', '\x31', '\x2', '\x2', 
		'?', '@', '\a', '\x31', '\x2', '\x2', '@', '\x10', '\x3', '\x2', '\x2', 
		'\x2', '\x41', '\x42', '\a', '\x31', '\x2', '\x2', '\x42', '\x12', '\x3', 
		'\x2', '\x2', '\x2', '\x43', '\x44', '\a', '\'', '\x2', '\x2', '\x44', 
		'\x14', '\x3', '\x2', '\x2', '\x2', '\x45', '\x46', '\a', '-', '\x2', 
		'\x2', '\x46', '\x16', '\x3', '\x2', '\x2', '\x2', 'G', 'H', '\a', '(', 
		'\x2', '\x2', 'H', '\x18', '\x3', '\x2', '\x2', '\x2', 'I', 'J', '\a', 
		'`', '\x2', '\x2', 'J', '\x1A', '\x3', '\x2', '\x2', '\x2', 'K', 'L', 
		'\a', '~', '\x2', '\x2', 'L', '\x1C', '\x3', '\x2', '\x2', '\x2', 'M', 
		'N', '\a', '.', '\x2', '\x2', 'N', '\x1E', '\x3', '\x2', '\x2', '\x2', 
		'O', 'Q', '\x5', '/', '\x18', '\x2', 'P', 'O', '\x3', '\x2', '\x2', '\x2', 
		'Q', 'R', '\x3', '\x2', '\x2', '\x2', 'R', 'P', '\x3', '\x2', '\x2', '\x2', 
		'R', 'S', '\x3', '\x2', '\x2', '\x2', 'S', 'T', '\x3', '\x2', '\x2', '\x2', 
		'T', 'U', '\b', '\x10', '\x2', '\x2', 'U', ' ', '\x3', '\x2', '\x2', '\x2', 
		'V', 'X', '\t', '\x2', '\x2', '\x2', 'W', 'V', '\x3', '\x2', '\x2', '\x2', 
		'X', 'Y', '\x3', '\x2', '\x2', '\x2', 'Y', 'W', '\x3', '\x2', '\x2', '\x2', 
		'Y', 'Z', '\x3', '\x2', '\x2', '\x2', 'Z', '[', '\x3', '\x2', '\x2', '\x2', 
		'[', ']', '\a', '\x30', '\x2', '\x2', '\\', '^', '\t', '\x2', '\x2', '\x2', 
		']', '\\', '\x3', '\x2', '\x2', '\x2', '^', '_', '\x3', '\x2', '\x2', 
		'\x2', '_', ']', '\x3', '\x2', '\x2', '\x2', '_', '`', '\x3', '\x2', '\x2', 
		'\x2', '`', '\"', '\x3', '\x2', '\x2', '\x2', '\x61', '\x65', '\t', '\x3', 
		'\x2', '\x2', '\x62', '\x64', '\t', '\x4', '\x2', '\x2', '\x63', '\x62', 
		'\x3', '\x2', '\x2', '\x2', '\x64', 'g', '\x3', '\x2', '\x2', '\x2', '\x65', 
		'\x63', '\x3', '\x2', '\x2', '\x2', '\x65', '\x66', '\x3', '\x2', '\x2', 
		'\x2', '\x66', '$', '\x3', '\x2', '\x2', '\x2', 'g', '\x65', '\x3', '\x2', 
		'\x2', '\x2', 'h', 'm', '\x5', '\'', '\x14', '\x2', 'i', 'm', '\x5', ')', 
		'\x15', '\x2', 'j', 'm', '\x5', '+', '\x16', '\x2', 'k', 'm', '\x5', '-', 
		'\x17', '\x2', 'l', 'h', '\x3', '\x2', '\x2', '\x2', 'l', 'i', '\x3', 
		'\x2', '\x2', '\x2', 'l', 'j', '\x3', '\x2', '\x2', '\x2', 'l', 'k', '\x3', 
		'\x2', '\x2', '\x2', 'm', '&', '\x3', '\x2', '\x2', '\x2', 'n', 'p', '\t', 
		'\x5', '\x2', '\x2', 'o', 'n', '\x3', '\x2', '\x2', '\x2', 'p', 'q', '\x3', 
		'\x2', '\x2', '\x2', 'q', 'o', '\x3', '\x2', '\x2', '\x2', 'q', 'r', '\x3', 
		'\x2', '\x2', '\x2', 'r', '(', '\x3', '\x2', '\x2', '\x2', 's', 't', '\a', 
		'\x32', '\x2', '\x2', 't', 'u', '\a', 'z', '\x2', '\x2', 'u', 'w', '\x3', 
		'\x2', '\x2', '\x2', 'v', 'x', '\t', '\x6', '\x2', '\x2', 'w', 'v', '\x3', 
		'\x2', '\x2', '\x2', 'x', 'y', '\x3', '\x2', '\x2', '\x2', 'y', 'w', '\x3', 
		'\x2', '\x2', '\x2', 'y', 'z', '\x3', '\x2', '\x2', '\x2', 'z', '*', '\x3', 
		'\x2', '\x2', '\x2', '{', '|', '\a', '\x32', '\x2', '\x2', '|', '}', '\a', 
		'q', '\x2', '\x2', '}', '\x7F', '\x3', '\x2', '\x2', '\x2', '~', '\x80', 
		'\t', '\a', '\x2', '\x2', '\x7F', '~', '\x3', '\x2', '\x2', '\x2', '\x80', 
		'\x81', '\x3', '\x2', '\x2', '\x2', '\x81', '\x7F', '\x3', '\x2', '\x2', 
		'\x2', '\x81', '\x82', '\x3', '\x2', '\x2', '\x2', '\x82', ',', '\x3', 
		'\x2', '\x2', '\x2', '\x83', '\x84', '\a', '\x32', '\x2', '\x2', '\x84', 
		'\x85', '\a', '\x64', '\x2', '\x2', '\x85', '\x87', '\x3', '\x2', '\x2', 
		'\x2', '\x86', '\x88', '\t', '\b', '\x2', '\x2', '\x87', '\x86', '\x3', 
		'\x2', '\x2', '\x2', '\x88', '\x89', '\x3', '\x2', '\x2', '\x2', '\x89', 
		'\x87', '\x3', '\x2', '\x2', '\x2', '\x89', '\x8A', '\x3', '\x2', '\x2', 
		'\x2', '\x8A', '.', '\x3', '\x2', '\x2', '\x2', '\x8B', '\x8C', '\t', 
		'\t', '\x2', '\x2', '\x8C', '\x30', '\x3', '\x2', '\x2', '\x2', '\f', 
		'\x2', 'R', 'Y', '_', '\x65', 'l', 'q', 'y', '\x81', '\x89', '\x3', '\x2', 
		'\x3', '\x2',
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace SharpIrcBot.Plugins.Calc.Language
