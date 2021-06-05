//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.9.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from CalcLang.g4 by ANTLR 4.9.1

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
using System.Diagnostics;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.9.1")]
[System.CLSCompliant(false)]
public partial class CalcLangParser : Parser {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, T__6=7, T__7=8, T__8=9, 
		T__9=10, T__10=11, T__11=12, T__12=13, T__13=14, Whitespaces=15, Decimal=16, 
		Identifier=17, Integer=18, Integer10=19, Integer16=20, Integer8=21, Integer2=22;
	public const int
		RULE_fullExpression = 0, RULE_expression = 1, RULE_arglist = 2;
	public static readonly string[] ruleNames = {
		"fullExpression", "expression", "arglist"
	};

	private static readonly string[] _LiteralNames = {
		null, "'('", "')'", "'!'", "'-'", "'**'", "'*'", "'/'", "'//'", "'%'", 
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

	public override string SerializedAtn { get { return new string(_serializedATN); } }

	static CalcLangParser() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}

		public CalcLangParser(ITokenStream input) : this(input, Console.Out, Console.Error) { }

		public CalcLangParser(ITokenStream input, TextWriter output, TextWriter errorOutput)
		: base(input, output, errorOutput)
	{
		Interpreter = new ParserATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	public partial class FullExpressionContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression() {
			return GetRuleContext<ExpressionContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Eof() { return GetToken(CalcLangParser.Eof, 0); }
		public FullExpressionContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_fullExpression; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterFullExpression(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitFullExpression(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitFullExpression(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public FullExpressionContext fullExpression() {
		FullExpressionContext _localctx = new FullExpressionContext(Context, State);
		EnterRule(_localctx, 0, RULE_fullExpression);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 6;
			expression(0);
			State = 7;
			Match(Eof);
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

	public partial class ExpressionContext : ParserRuleContext {
		public ExpressionContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_expression; } }
	 
		public ExpressionContext() { }
		public virtual void CopyFrom(ExpressionContext context) {
			base.CopyFrom(context);
		}
	}
	public partial class DecContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Decimal() { return GetToken(CalcLangParser.Decimal, 0); }
		public DecContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterDec(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitDec(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitDec(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class BOrContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext[] expression() {
			return GetRuleContexts<ExpressionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression(int i) {
			return GetRuleContext<ExpressionContext>(i);
		}
		public BOrContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterBOr(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitBOr(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitBOr(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class FuncContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Identifier() { return GetToken(CalcLangParser.Identifier, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public ArglistContext arglist() {
			return GetRuleContext<ArglistContext>(0);
		}
		public FuncContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterFunc(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitFunc(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitFunc(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class CstContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Identifier() { return GetToken(CalcLangParser.Identifier, 0); }
		public CstContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterCst(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitCst(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitCst(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class AddSubContext : ExpressionContext {
		public IToken op;
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext[] expression() {
			return GetRuleContexts<ExpressionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression(int i) {
			return GetRuleContext<ExpressionContext>(i);
		}
		public AddSubContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterAddSub(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitAddSub(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitAddSub(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class ParensContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression() {
			return GetRuleContext<ExpressionContext>(0);
		}
		public ParensContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterParens(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitParens(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitParens(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class BXorContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext[] expression() {
			return GetRuleContexts<ExpressionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression(int i) {
			return GetRuleContext<ExpressionContext>(i);
		}
		public BXorContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterBXor(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitBXor(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitBXor(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class FacContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression() {
			return GetRuleContext<ExpressionContext>(0);
		}
		public FacContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterFac(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitFac(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitFac(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class MulDivRemContext : ExpressionContext {
		public IToken op;
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext[] expression() {
			return GetRuleContexts<ExpressionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression(int i) {
			return GetRuleContext<ExpressionContext>(i);
		}
		public MulDivRemContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterMulDivRem(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitMulDivRem(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitMulDivRem(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class BAndContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext[] expression() {
			return GetRuleContexts<ExpressionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression(int i) {
			return GetRuleContext<ExpressionContext>(i);
		}
		public BAndContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterBAnd(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitBAnd(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitBAnd(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class IntContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Integer() { return GetToken(CalcLangParser.Integer, 0); }
		public IntContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterInt(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitInt(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitInt(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class NegContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression() {
			return GetRuleContext<ExpressionContext>(0);
		}
		public NegContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterNeg(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitNeg(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitNeg(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class PowContext : ExpressionContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext[] expression() {
			return GetRuleContexts<ExpressionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression(int i) {
			return GetRuleContext<ExpressionContext>(i);
		}
		public PowContext(ExpressionContext context) { CopyFrom(context); }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterPow(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitPow(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitPow(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ExpressionContext expression() {
		return expression(0);
	}

	private ExpressionContext expression(int _p) {
		ParserRuleContext _parentctx = Context;
		int _parentState = State;
		ExpressionContext _localctx = new ExpressionContext(Context, _parentState);
		ExpressionContext _prevctx = _localctx;
		int _startState = 2;
		EnterRecursionRule(_localctx, 2, RULE_expression, _p);
		int _la;
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 25;
			ErrorHandler.Sync(this);
			switch ( Interpreter.AdaptivePredict(TokenStream,1,Context) ) {
			case 1:
				{
				_localctx = new ParensContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;

				State = 10;
				Match(T__0);
				State = 11;
				expression(0);
				State = 12;
				Match(T__1);
				}
				break;
			case 2:
				{
				_localctx = new FuncContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 14;
				Match(Identifier);
				State = 15;
				Match(T__0);
				State = 17;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__0) | (1L << T__3) | (1L << Decimal) | (1L << Identifier) | (1L << Integer))) != 0)) {
					{
					State = 16;
					arglist();
					}
				}

				State = 19;
				Match(T__1);
				}
				break;
			case 3:
				{
				_localctx = new NegContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 20;
				Match(T__3);
				State = 21;
				expression(10);
				}
				break;
			case 4:
				{
				_localctx = new CstContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 22;
				Match(Identifier);
				}
				break;
			case 5:
				{
				_localctx = new IntContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 23;
				Match(Integer);
				}
				break;
			case 6:
				{
				_localctx = new DecContext(_localctx);
				Context = _localctx;
				_prevctx = _localctx;
				State = 24;
				Match(Decimal);
				}
				break;
			}
			Context.Stop = TokenStream.LT(-1);
			State = 49;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,3,Context);
			while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( ParseListeners!=null )
						TriggerExitRuleEvent();
					_prevctx = _localctx;
					{
					State = 47;
					ErrorHandler.Sync(this);
					switch ( Interpreter.AdaptivePredict(TokenStream,2,Context) ) {
					case 1:
						{
						_localctx = new PowContext(new ExpressionContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 27;
						if (!(Precpred(Context, 9))) throw new FailedPredicateException(this, "Precpred(Context, 9)");
						State = 28;
						Match(T__4);
						State = 29;
						expression(9);
						}
						break;
					case 2:
						{
						_localctx = new MulDivRemContext(new ExpressionContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 30;
						if (!(Precpred(Context, 8))) throw new FailedPredicateException(this, "Precpred(Context, 8)");
						State = 31;
						((MulDivRemContext)_localctx).op = TokenStream.LT(1);
						_la = TokenStream.LA(1);
						if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__5) | (1L << T__6) | (1L << T__7) | (1L << T__8))) != 0)) ) {
							((MulDivRemContext)_localctx).op = ErrorHandler.RecoverInline(this);
						}
						else {
							ErrorHandler.ReportMatch(this);
						    Consume();
						}
						State = 32;
						expression(9);
						}
						break;
					case 3:
						{
						_localctx = new AddSubContext(new ExpressionContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 33;
						if (!(Precpred(Context, 7))) throw new FailedPredicateException(this, "Precpred(Context, 7)");
						State = 34;
						((AddSubContext)_localctx).op = TokenStream.LT(1);
						_la = TokenStream.LA(1);
						if ( !(_la==T__3 || _la==T__9) ) {
							((AddSubContext)_localctx).op = ErrorHandler.RecoverInline(this);
						}
						else {
							ErrorHandler.ReportMatch(this);
						    Consume();
						}
						State = 35;
						expression(8);
						}
						break;
					case 4:
						{
						_localctx = new BAndContext(new ExpressionContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 36;
						if (!(Precpred(Context, 6))) throw new FailedPredicateException(this, "Precpred(Context, 6)");
						State = 37;
						Match(T__10);
						State = 38;
						expression(7);
						}
						break;
					case 5:
						{
						_localctx = new BXorContext(new ExpressionContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 39;
						if (!(Precpred(Context, 5))) throw new FailedPredicateException(this, "Precpred(Context, 5)");
						State = 40;
						Match(T__11);
						State = 41;
						expression(6);
						}
						break;
					case 6:
						{
						_localctx = new BOrContext(new ExpressionContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 42;
						if (!(Precpred(Context, 4))) throw new FailedPredicateException(this, "Precpred(Context, 4)");
						State = 43;
						Match(T__12);
						State = 44;
						expression(5);
						}
						break;
					case 7:
						{
						_localctx = new FacContext(new ExpressionContext(_parentctx, _parentState));
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 45;
						if (!(Precpred(Context, 11))) throw new FailedPredicateException(this, "Precpred(Context, 11)");
						State = 46;
						Match(T__2);
						}
						break;
					}
					} 
				}
				State = 51;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,3,Context);
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

	public partial class ArglistContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression() {
			return GetRuleContext<ExpressionContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ArglistContext arglist() {
			return GetRuleContext<ArglistContext>(0);
		}
		public ArglistContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_arglist; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.EnterArglist(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ICalcLangListener typedListener = listener as ICalcLangListener;
			if (typedListener != null) typedListener.ExitArglist(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICalcLangVisitor<TResult> typedVisitor = visitor as ICalcLangVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitArglist(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ArglistContext arglist() {
		ArglistContext _localctx = new ArglistContext(Context, State);
		EnterRule(_localctx, 4, RULE_arglist);
		try {
			State = 57;
			ErrorHandler.Sync(this);
			switch ( Interpreter.AdaptivePredict(TokenStream,4,Context) ) {
			case 1:
				EnterOuterAlt(_localctx, 1);
				{
				State = 52;
				expression(0);
				State = 53;
				Match(T__13);
				State = 54;
				arglist();
				}
				break;
			case 2:
				EnterOuterAlt(_localctx, 2);
				{
				State = 56;
				expression(0);
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

	public override bool Sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 1: return expression_sempred((ExpressionContext)_localctx, predIndex);
		}
		return true;
	}
	private bool expression_sempred(ExpressionContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0: return Precpred(Context, 9);
		case 1: return Precpred(Context, 8);
		case 2: return Precpred(Context, 7);
		case 3: return Precpred(Context, 6);
		case 4: return Precpred(Context, 5);
		case 5: return Precpred(Context, 4);
		case 6: return Precpred(Context, 11);
		}
		return true;
	}

	private static char[] _serializedATN = {
		'\x3', '\x608B', '\xA72A', '\x8133', '\xB9ED', '\x417C', '\x3BE7', '\x7786', 
		'\x5964', '\x3', '\x18', '>', '\x4', '\x2', '\t', '\x2', '\x4', '\x3', 
		'\t', '\x3', '\x4', '\x4', '\t', '\x4', '\x3', '\x2', '\x3', '\x2', '\x3', 
		'\x2', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x5', '\x3', '\x14', 
		'\n', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x5', '\x3', '\x1C', '\n', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\a', '\x3', '\x32', 
		'\n', '\x3', '\f', '\x3', '\xE', '\x3', '\x35', '\v', '\x3', '\x3', '\x4', 
		'\x3', '\x4', '\x3', '\x4', '\x3', '\x4', '\x3', '\x4', '\x5', '\x4', 
		'<', '\n', '\x4', '\x3', '\x4', '\x2', '\x3', '\x4', '\x5', '\x2', '\x4', 
		'\x6', '\x2', '\x4', '\x3', '\x2', '\b', '\v', '\x4', '\x2', '\x6', '\x6', 
		'\f', '\f', '\x2', 'H', '\x2', '\b', '\x3', '\x2', '\x2', '\x2', '\x4', 
		'\x1B', '\x3', '\x2', '\x2', '\x2', '\x6', ';', '\x3', '\x2', '\x2', '\x2', 
		'\b', '\t', '\x5', '\x4', '\x3', '\x2', '\t', '\n', '\a', '\x2', '\x2', 
		'\x3', '\n', '\x3', '\x3', '\x2', '\x2', '\x2', '\v', '\f', '\b', '\x3', 
		'\x1', '\x2', '\f', '\r', '\a', '\x3', '\x2', '\x2', '\r', '\xE', '\x5', 
		'\x4', '\x3', '\x2', '\xE', '\xF', '\a', '\x4', '\x2', '\x2', '\xF', '\x1C', 
		'\x3', '\x2', '\x2', '\x2', '\x10', '\x11', '\a', '\x13', '\x2', '\x2', 
		'\x11', '\x13', '\a', '\x3', '\x2', '\x2', '\x12', '\x14', '\x5', '\x6', 
		'\x4', '\x2', '\x13', '\x12', '\x3', '\x2', '\x2', '\x2', '\x13', '\x14', 
		'\x3', '\x2', '\x2', '\x2', '\x14', '\x15', '\x3', '\x2', '\x2', '\x2', 
		'\x15', '\x1C', '\a', '\x4', '\x2', '\x2', '\x16', '\x17', '\a', '\x6', 
		'\x2', '\x2', '\x17', '\x1C', '\x5', '\x4', '\x3', '\f', '\x18', '\x1C', 
		'\a', '\x13', '\x2', '\x2', '\x19', '\x1C', '\a', '\x14', '\x2', '\x2', 
		'\x1A', '\x1C', '\a', '\x12', '\x2', '\x2', '\x1B', '\v', '\x3', '\x2', 
		'\x2', '\x2', '\x1B', '\x10', '\x3', '\x2', '\x2', '\x2', '\x1B', '\x16', 
		'\x3', '\x2', '\x2', '\x2', '\x1B', '\x18', '\x3', '\x2', '\x2', '\x2', 
		'\x1B', '\x19', '\x3', '\x2', '\x2', '\x2', '\x1B', '\x1A', '\x3', '\x2', 
		'\x2', '\x2', '\x1C', '\x33', '\x3', '\x2', '\x2', '\x2', '\x1D', '\x1E', 
		'\f', '\v', '\x2', '\x2', '\x1E', '\x1F', '\a', '\a', '\x2', '\x2', '\x1F', 
		'\x32', '\x5', '\x4', '\x3', '\v', ' ', '!', '\f', '\n', '\x2', '\x2', 
		'!', '\"', '\t', '\x2', '\x2', '\x2', '\"', '\x32', '\x5', '\x4', '\x3', 
		'\v', '#', '$', '\f', '\t', '\x2', '\x2', '$', '%', '\t', '\x3', '\x2', 
		'\x2', '%', '\x32', '\x5', '\x4', '\x3', '\n', '&', '\'', '\f', '\b', 
		'\x2', '\x2', '\'', '(', '\a', '\r', '\x2', '\x2', '(', '\x32', '\x5', 
		'\x4', '\x3', '\t', ')', '*', '\f', '\a', '\x2', '\x2', '*', '+', '\a', 
		'\xE', '\x2', '\x2', '+', '\x32', '\x5', '\x4', '\x3', '\b', ',', '-', 
		'\f', '\x6', '\x2', '\x2', '-', '.', '\a', '\xF', '\x2', '\x2', '.', '\x32', 
		'\x5', '\x4', '\x3', '\a', '/', '\x30', '\f', '\r', '\x2', '\x2', '\x30', 
		'\x32', '\a', '\x5', '\x2', '\x2', '\x31', '\x1D', '\x3', '\x2', '\x2', 
		'\x2', '\x31', ' ', '\x3', '\x2', '\x2', '\x2', '\x31', '#', '\x3', '\x2', 
		'\x2', '\x2', '\x31', '&', '\x3', '\x2', '\x2', '\x2', '\x31', ')', '\x3', 
		'\x2', '\x2', '\x2', '\x31', ',', '\x3', '\x2', '\x2', '\x2', '\x31', 
		'/', '\x3', '\x2', '\x2', '\x2', '\x32', '\x35', '\x3', '\x2', '\x2', 
		'\x2', '\x33', '\x31', '\x3', '\x2', '\x2', '\x2', '\x33', '\x34', '\x3', 
		'\x2', '\x2', '\x2', '\x34', '\x5', '\x3', '\x2', '\x2', '\x2', '\x35', 
		'\x33', '\x3', '\x2', '\x2', '\x2', '\x36', '\x37', '\x5', '\x4', '\x3', 
		'\x2', '\x37', '\x38', '\a', '\x10', '\x2', '\x2', '\x38', '\x39', '\x5', 
		'\x6', '\x4', '\x2', '\x39', '<', '\x3', '\x2', '\x2', '\x2', ':', '<', 
		'\x5', '\x4', '\x3', '\x2', ';', '\x36', '\x3', '\x2', '\x2', '\x2', ';', 
		':', '\x3', '\x2', '\x2', '\x2', '<', '\a', '\x3', '\x2', '\x2', '\x2', 
		'\a', '\x13', '\x1B', '\x31', '\x33', ';',
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace SharpIrcBot.Plugins.Calc.Language
