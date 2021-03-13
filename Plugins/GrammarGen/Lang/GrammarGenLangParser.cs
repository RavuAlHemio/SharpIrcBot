//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.9.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from GrammarGenLang.g4 by ANTLR 4.9.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace SharpIrcBot.Plugins.GrammarGen.Lang {
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.9.1")]
[System.CLSCompliant(false)]
public partial class GrammarGenLangParser : Parser {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, T__6=7, T__7=8, T__8=9, 
		T__9=10, T__10=11, T__11=12, Whitespaces=13, Comments=14, LineComments=15, 
		EscapedString=16, Identifier=17;
	public const int
		RULE_ggrulebook = 0, RULE_ruledef = 1, RULE_ggrule = 2, RULE_paramrule = 3, 
		RULE_ggproduction = 4, RULE_alternative = 5, RULE_sequenceElem = 6;
	public static readonly string[] ruleNames = {
		"ggrulebook", "ruledef", "ggrule", "paramrule", "ggproduction", "alternative", 
		"sequenceElem"
	};

	private static readonly string[] _LiteralNames = {
		null, "':'", "';'", "'{'", "','", "'}'", "'|'", "'('", "')'", "'['", "']'", 
		"'*'", "'+'"
	};
	private static readonly string[] _SymbolicNames = {
		null, null, null, null, null, null, null, null, null, null, null, null, 
		null, "Whitespaces", "Comments", "LineComments", "EscapedString", "Identifier"
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

	public override string GrammarFileName { get { return "GrammarGenLang.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string SerializedAtn { get { return new string(_serializedATN); } }

	static GrammarGenLangParser() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}

		public GrammarGenLangParser(ITokenStream input) : this(input, Console.Out, Console.Error) { }

		public GrammarGenLangParser(ITokenStream input, TextWriter output, TextWriter errorOutput)
		: base(input, output, errorOutput)
	{
		Interpreter = new ParserATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	public partial class GgrulebookContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public RuledefContext[] ruledef() {
			return GetRuleContexts<RuledefContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public RuledefContext ruledef(int i) {
			return GetRuleContext<RuledefContext>(i);
		}
		public GgrulebookContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_ggrulebook; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterGgrulebook(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitGgrulebook(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitGgrulebook(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public GgrulebookContext ggrulebook() {
		GgrulebookContext _localctx = new GgrulebookContext(Context, State);
		EnterRule(_localctx, 0, RULE_ggrulebook);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 15;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			do {
				{
				{
				State = 14;
				ruledef();
				}
				}
				State = 17;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			} while ( _la==Identifier );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class RuledefContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public GgruleContext ggrule() {
			return GetRuleContext<GgruleContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ParamruleContext paramrule() {
			return GetRuleContext<ParamruleContext>(0);
		}
		public RuledefContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_ruledef; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterRuledef(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitRuledef(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitRuledef(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public RuledefContext ruledef() {
		RuledefContext _localctx = new RuledefContext(Context, State);
		EnterRule(_localctx, 2, RULE_ruledef);
		try {
			State = 21;
			ErrorHandler.Sync(this);
			switch ( Interpreter.AdaptivePredict(TokenStream,1,Context) ) {
			case 1:
				EnterOuterAlt(_localctx, 1);
				{
				State = 19;
				ggrule();
				}
				break;
			case 2:
				EnterOuterAlt(_localctx, 2);
				{
				State = 20;
				paramrule();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class GgruleContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Identifier() { return GetToken(GrammarGenLangParser.Identifier, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public GgproductionContext ggproduction() {
			return GetRuleContext<GgproductionContext>(0);
		}
		public GgruleContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_ggrule; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterGgrule(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitGgrule(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitGgrule(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public GgruleContext ggrule() {
		GgruleContext _localctx = new GgruleContext(Context, State);
		EnterRule(_localctx, 4, RULE_ggrule);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 23;
			Match(Identifier);
			State = 24;
			Match(T__0);
			State = 25;
			ggproduction();
			State = 26;
			Match(T__1);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class ParamruleContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode[] Identifier() { return GetTokens(GrammarGenLangParser.Identifier); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Identifier(int i) {
			return GetToken(GrammarGenLangParser.Identifier, i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public GgproductionContext ggproduction() {
			return GetRuleContext<GgproductionContext>(0);
		}
		public ParamruleContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_paramrule; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterParamrule(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitParamrule(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitParamrule(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ParamruleContext paramrule() {
		ParamruleContext _localctx = new ParamruleContext(Context, State);
		EnterRule(_localctx, 6, RULE_paramrule);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 28;
			Match(Identifier);
			State = 29;
			Match(T__2);
			State = 30;
			Match(Identifier);
			State = 33;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			do {
				{
				{
				State = 31;
				Match(T__3);
				State = 32;
				Match(Identifier);
				}
				}
				State = 35;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			} while ( _la==T__3 );
			State = 37;
			Match(T__4);
			State = 38;
			Match(T__0);
			State = 39;
			ggproduction();
			State = 40;
			Match(T__1);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class GgproductionContext : ParserRuleContext {
		public GgproductionContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_ggproduction; } }
	 
		public GgproductionContext() { }
		public virtual void CopyFrom(GgproductionContext context) {
			base.CopyFrom(context);
		}
	}
	public partial class AlternContext : GgproductionContext {
		[System.Diagnostics.DebuggerNonUserCode] public AlternativeContext[] alternative() {
			return GetRuleContexts<AlternativeContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public AlternativeContext alternative(int i) {
			return GetRuleContext<AlternativeContext>(i);
		}
		public AlternContext(GgproductionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterAltern(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitAltern(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitAltern(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public GgproductionContext ggproduction() {
		GgproductionContext _localctx = new GgproductionContext(Context, State);
		EnterRule(_localctx, 8, RULE_ggproduction);
		int _la;
		try {
			_localctx = new AlternContext(_localctx);
			EnterOuterAlt(_localctx, 1);
			{
			State = 42;
			alternative();
			State = 47;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			while (_la==T__5) {
				{
				{
				State = 43;
				Match(T__5);
				State = 44;
				alternative();
				}
				}
				State = 49;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class AlternativeContext : ParserRuleContext {
		public AlternativeContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_alternative; } }
	 
		public AlternativeContext() { }
		public virtual void CopyFrom(AlternativeContext context) {
			base.CopyFrom(context);
		}
	}
	public partial class SeqContext : AlternativeContext {
		[System.Diagnostics.DebuggerNonUserCode] public SequenceElemContext[] sequenceElem() {
			return GetRuleContexts<SequenceElemContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public SequenceElemContext sequenceElem(int i) {
			return GetRuleContext<SequenceElemContext>(i);
		}
		public SeqContext(AlternativeContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterSeq(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitSeq(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitSeq(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public AlternativeContext alternative() {
		AlternativeContext _localctx = new AlternativeContext(Context, State);
		EnterRule(_localctx, 10, RULE_alternative);
		int _la;
		try {
			_localctx = new SeqContext(_localctx);
			EnterOuterAlt(_localctx, 1);
			{
			State = 51;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			do {
				{
				{
				State = 50;
				sequenceElem(0);
				}
				}
				State = 53;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			} while ( (((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__6) | (1L << T__8) | (1L << EscapedString) | (1L << Identifier))) != 0) );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class SequenceElemContext : ParserRuleContext {
		public SequenceElemContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_sequenceElem; } }
	 
		public SequenceElemContext() { }
		public virtual void CopyFrom(SequenceElemContext context) {
			base.CopyFrom(context);
		}
	}
	public partial class StrContext : SequenceElemContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode EscapedString() { return GetToken(GrammarGenLangParser.EscapedString, 0); }
		public StrContext(SequenceElemContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterStr(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitStr(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitStr(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class GroupContext : SequenceElemContext {
		[System.Diagnostics.DebuggerNonUserCode] public GgproductionContext ggproduction() {
			return GetRuleContext<GgproductionContext>(0);
		}
		public GroupContext(SequenceElemContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterGroup(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitGroup(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitGroup(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class CallContext : SequenceElemContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Identifier() { return GetToken(GrammarGenLangParser.Identifier, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public GgproductionContext[] ggproduction() {
			return GetRuleContexts<GgproductionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public GgproductionContext ggproduction(int i) {
			return GetRuleContext<GgproductionContext>(i);
		}
		public CallContext(SequenceElemContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterCall(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitCall(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitCall(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class OptContext : SequenceElemContext {
		[System.Diagnostics.DebuggerNonUserCode] public GgproductionContext ggproduction() {
			return GetRuleContext<GgproductionContext>(0);
		}
		public OptContext(SequenceElemContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterOpt(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitOpt(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitOpt(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class IdentContext : SequenceElemContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Identifier() { return GetToken(GrammarGenLangParser.Identifier, 0); }
		public IdentContext(SequenceElemContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterIdent(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitIdent(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitIdent(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class StarContext : SequenceElemContext {
		[System.Diagnostics.DebuggerNonUserCode] public SequenceElemContext sequenceElem() {
			return GetRuleContext<SequenceElemContext>(0);
		}
		public StarContext(SequenceElemContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterStar(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitStar(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitStar(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class PlusContext : SequenceElemContext {
		[System.Diagnostics.DebuggerNonUserCode] public SequenceElemContext sequenceElem() {
			return GetRuleContext<SequenceElemContext>(0);
		}
		public PlusContext(SequenceElemContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.EnterPlus(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IGrammarGenLangListener typedListener = listener as IGrammarGenLangListener;
			if (typedListener != null) typedListener.ExitPlus(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IGrammarGenLangVisitor<TResult> typedVisitor = visitor as IGrammarGenLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitPlus(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public SequenceElemContext sequenceElem() {
		return sequenceElem(0);
	}

	private SequenceElemContext sequenceElem(int _p) {
		ParserRuleContext _parentctx = Context;
		int _parentState = State;
		SequenceElemContext _localctx = new SequenceElemContext(Context, _parentState);
		SequenceElemContext _prevctx = _localctx;
		int _startState = 12;
		EnterRecursionRule(_localctx, 12, RULE_sequenceElem, _p);
		int _la;
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 78;
			ErrorHandler.Sync(this);
			switch ( Interpreter.AdaptivePredict(TokenStream,6,Context) ) {
			case 1:
				{
				_localctx = new GroupContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;

				State = 56;
				Match(T__6);
				State = 57;
				ggproduction();
				State = 58;
				Match(T__7);
				}
				break;
			case 2:
				{
				_localctx = new OptContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 60;
				Match(T__8);
				State = 61;
				ggproduction();
				State = 62;
				Match(T__9);
				}
				break;
			case 3:
				{
				_localctx = new CallContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 64;
				Match(Identifier);
				State = 65;
				Match(T__2);
				State = 66;
				ggproduction();
				State = 71;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				while (_la==T__3) {
					{
					{
					State = 67;
					Match(T__3);
					State = 68;
					ggproduction();
					}
					}
					State = 73;
					ErrorHandler.Sync(this);
					_la = TokenStream.LA(1);
				}
				State = 74;
				Match(T__4);
				}
				break;
			case 4:
				{
				_localctx = new IdentContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 76;
				Match(Identifier);
				}
				break;
			case 5:
				{
				_localctx = new StrContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 77;
				Match(EscapedString);
				}
				break;
			}
			Context.Stop = TokenStream.LT(-1);
			State = 86;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,8,Context);
			while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( ParseListeners!=null )
						TriggerExitRuleEvent();
					_prevctx = _localctx;
					{
					State = 84;
					ErrorHandler.Sync(this);
					switch ( Interpreter.AdaptivePredict(TokenStream,7,Context) ) {
					case 1:
						{
						_localctx = new StarContext(new SequenceElemContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_sequenceElem);
						State = 80;
						if (!(Precpred(Context, 5))) throw new FailedPredicateException(this, "Precpred(Context, 5)");
						State = 81;
						Match(T__10);
						}
						break;
					case 2:
						{
						_localctx = new PlusContext(new SequenceElemContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_sequenceElem);
						State = 82;
						if (!(Precpred(Context, 4))) throw new FailedPredicateException(this, "Precpred(Context, 4)");
						State = 83;
						Match(T__11);
						}
						break;
					}
					} 
				}
				State = 88;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,8,Context);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			UnrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	public override bool Sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 6: return sequenceElem_sempred((SequenceElemContext)_localctx, predIndex);
		}
		return true;
	}
	private bool sequenceElem_sempred(SequenceElemContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0: return Precpred(Context, 5);
		case 1: return Precpred(Context, 4);
		}
		return true;
	}

	private static char[] _serializedATN = {
		'\x3', '\x608B', '\xA72A', '\x8133', '\xB9ED', '\x417C', '\x3BE7', '\x7786', 
		'\x5964', '\x3', '\x13', '\\', '\x4', '\x2', '\t', '\x2', '\x4', '\x3', 
		'\t', '\x3', '\x4', '\x4', '\t', '\x4', '\x4', '\x5', '\t', '\x5', '\x4', 
		'\x6', '\t', '\x6', '\x4', '\a', '\t', '\a', '\x4', '\b', '\t', '\b', 
		'\x3', '\x2', '\x6', '\x2', '\x12', '\n', '\x2', '\r', '\x2', '\xE', '\x2', 
		'\x13', '\x3', '\x3', '\x3', '\x3', '\x5', '\x3', '\x18', '\n', '\x3', 
		'\x3', '\x4', '\x3', '\x4', '\x3', '\x4', '\x3', '\x4', '\x3', '\x4', 
		'\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', 
		'\x6', '\x5', '$', '\n', '\x5', '\r', '\x5', '\xE', '\x5', '%', '\x3', 
		'\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', 
		'\x6', '\x3', '\x6', '\x3', '\x6', '\a', '\x6', '\x30', '\n', '\x6', '\f', 
		'\x6', '\xE', '\x6', '\x33', '\v', '\x6', '\x3', '\a', '\x6', '\a', '\x36', 
		'\n', '\a', '\r', '\a', '\xE', '\a', '\x37', '\x3', '\b', '\x3', '\b', 
		'\x3', '\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', '\x3', 
		'\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', 
		'\x3', '\b', '\a', '\b', 'H', '\n', '\b', '\f', '\b', '\xE', '\b', 'K', 
		'\v', '\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', '\x5', 
		'\b', 'Q', '\n', '\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', '\x3', '\b', 
		'\a', '\b', 'W', '\n', '\b', '\f', '\b', '\xE', '\b', 'Z', '\v', '\b', 
		'\x3', '\b', '\x2', '\x3', '\xE', '\t', '\x2', '\x4', '\x6', '\b', '\n', 
		'\f', '\xE', '\x2', '\x2', '\x2', '`', '\x2', '\x11', '\x3', '\x2', '\x2', 
		'\x2', '\x4', '\x17', '\x3', '\x2', '\x2', '\x2', '\x6', '\x19', '\x3', 
		'\x2', '\x2', '\x2', '\b', '\x1E', '\x3', '\x2', '\x2', '\x2', '\n', ',', 
		'\x3', '\x2', '\x2', '\x2', '\f', '\x35', '\x3', '\x2', '\x2', '\x2', 
		'\xE', 'P', '\x3', '\x2', '\x2', '\x2', '\x10', '\x12', '\x5', '\x4', 
		'\x3', '\x2', '\x11', '\x10', '\x3', '\x2', '\x2', '\x2', '\x12', '\x13', 
		'\x3', '\x2', '\x2', '\x2', '\x13', '\x11', '\x3', '\x2', '\x2', '\x2', 
		'\x13', '\x14', '\x3', '\x2', '\x2', '\x2', '\x14', '\x3', '\x3', '\x2', 
		'\x2', '\x2', '\x15', '\x18', '\x5', '\x6', '\x4', '\x2', '\x16', '\x18', 
		'\x5', '\b', '\x5', '\x2', '\x17', '\x15', '\x3', '\x2', '\x2', '\x2', 
		'\x17', '\x16', '\x3', '\x2', '\x2', '\x2', '\x18', '\x5', '\x3', '\x2', 
		'\x2', '\x2', '\x19', '\x1A', '\a', '\x13', '\x2', '\x2', '\x1A', '\x1B', 
		'\a', '\x3', '\x2', '\x2', '\x1B', '\x1C', '\x5', '\n', '\x6', '\x2', 
		'\x1C', '\x1D', '\a', '\x4', '\x2', '\x2', '\x1D', '\a', '\x3', '\x2', 
		'\x2', '\x2', '\x1E', '\x1F', '\a', '\x13', '\x2', '\x2', '\x1F', ' ', 
		'\a', '\x5', '\x2', '\x2', ' ', '#', '\a', '\x13', '\x2', '\x2', '!', 
		'\"', '\a', '\x6', '\x2', '\x2', '\"', '$', '\a', '\x13', '\x2', '\x2', 
		'#', '!', '\x3', '\x2', '\x2', '\x2', '$', '%', '\x3', '\x2', '\x2', '\x2', 
		'%', '#', '\x3', '\x2', '\x2', '\x2', '%', '&', '\x3', '\x2', '\x2', '\x2', 
		'&', '\'', '\x3', '\x2', '\x2', '\x2', '\'', '(', '\a', '\a', '\x2', '\x2', 
		'(', ')', '\a', '\x3', '\x2', '\x2', ')', '*', '\x5', '\n', '\x6', '\x2', 
		'*', '+', '\a', '\x4', '\x2', '\x2', '+', '\t', '\x3', '\x2', '\x2', '\x2', 
		',', '\x31', '\x5', '\f', '\a', '\x2', '-', '.', '\a', '\b', '\x2', '\x2', 
		'.', '\x30', '\x5', '\f', '\a', '\x2', '/', '-', '\x3', '\x2', '\x2', 
		'\x2', '\x30', '\x33', '\x3', '\x2', '\x2', '\x2', '\x31', '/', '\x3', 
		'\x2', '\x2', '\x2', '\x31', '\x32', '\x3', '\x2', '\x2', '\x2', '\x32', 
		'\v', '\x3', '\x2', '\x2', '\x2', '\x33', '\x31', '\x3', '\x2', '\x2', 
		'\x2', '\x34', '\x36', '\x5', '\xE', '\b', '\x2', '\x35', '\x34', '\x3', 
		'\x2', '\x2', '\x2', '\x36', '\x37', '\x3', '\x2', '\x2', '\x2', '\x37', 
		'\x35', '\x3', '\x2', '\x2', '\x2', '\x37', '\x38', '\x3', '\x2', '\x2', 
		'\x2', '\x38', '\r', '\x3', '\x2', '\x2', '\x2', '\x39', ':', '\b', '\b', 
		'\x1', '\x2', ':', ';', '\a', '\t', '\x2', '\x2', ';', '<', '\x5', '\n', 
		'\x6', '\x2', '<', '=', '\a', '\n', '\x2', '\x2', '=', 'Q', '\x3', '\x2', 
		'\x2', '\x2', '>', '?', '\a', '\v', '\x2', '\x2', '?', '@', '\x5', '\n', 
		'\x6', '\x2', '@', '\x41', '\a', '\f', '\x2', '\x2', '\x41', 'Q', '\x3', 
		'\x2', '\x2', '\x2', '\x42', '\x43', '\a', '\x13', '\x2', '\x2', '\x43', 
		'\x44', '\a', '\x5', '\x2', '\x2', '\x44', 'I', '\x5', '\n', '\x6', '\x2', 
		'\x45', '\x46', '\a', '\x6', '\x2', '\x2', '\x46', 'H', '\x5', '\n', '\x6', 
		'\x2', 'G', '\x45', '\x3', '\x2', '\x2', '\x2', 'H', 'K', '\x3', '\x2', 
		'\x2', '\x2', 'I', 'G', '\x3', '\x2', '\x2', '\x2', 'I', 'J', '\x3', '\x2', 
		'\x2', '\x2', 'J', 'L', '\x3', '\x2', '\x2', '\x2', 'K', 'I', '\x3', '\x2', 
		'\x2', '\x2', 'L', 'M', '\a', '\a', '\x2', '\x2', 'M', 'Q', '\x3', '\x2', 
		'\x2', '\x2', 'N', 'Q', '\a', '\x13', '\x2', '\x2', 'O', 'Q', '\a', '\x12', 
		'\x2', '\x2', 'P', '\x39', '\x3', '\x2', '\x2', '\x2', 'P', '>', '\x3', 
		'\x2', '\x2', '\x2', 'P', '\x42', '\x3', '\x2', '\x2', '\x2', 'P', 'N', 
		'\x3', '\x2', '\x2', '\x2', 'P', 'O', '\x3', '\x2', '\x2', '\x2', 'Q', 
		'X', '\x3', '\x2', '\x2', '\x2', 'R', 'S', '\f', '\a', '\x2', '\x2', 'S', 
		'W', '\a', '\r', '\x2', '\x2', 'T', 'U', '\f', '\x6', '\x2', '\x2', 'U', 
		'W', '\a', '\xE', '\x2', '\x2', 'V', 'R', '\x3', '\x2', '\x2', '\x2', 
		'V', 'T', '\x3', '\x2', '\x2', '\x2', 'W', 'Z', '\x3', '\x2', '\x2', '\x2', 
		'X', 'V', '\x3', '\x2', '\x2', '\x2', 'X', 'Y', '\x3', '\x2', '\x2', '\x2', 
		'Y', '\xF', '\x3', '\x2', '\x2', '\x2', 'Z', 'X', '\x3', '\x2', '\x2', 
		'\x2', '\v', '\x13', '\x17', '%', '\x31', '\x37', 'I', 'P', 'V', 'X',
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace SharpIrcBot.Plugins.GrammarGen.Lang