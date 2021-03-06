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

using Antlr4.Runtime.Misc;
using IErrorNode = Antlr4.Runtime.Tree.IErrorNode;
using ITerminalNode = Antlr4.Runtime.Tree.ITerminalNode;
using IToken = Antlr4.Runtime.IToken;
using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;

/// <summary>
/// This class provides an empty implementation of <see cref="IGrammarGenLangListener"/>,
/// which can be extended to create a listener which only needs to handle a subset
/// of the available methods.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.9.1")]
[System.Diagnostics.DebuggerNonUserCode]
[System.CLSCompliant(false)]
public partial class GrammarGenLangBaseListener : IGrammarGenLangListener {
	/// <summary>
	/// Enter a parse tree produced by <see cref="GrammarGenLangParser.ggrulebook"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterGgrulebook([NotNull] GrammarGenLangParser.GgrulebookContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="GrammarGenLangParser.ggrulebook"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitGgrulebook([NotNull] GrammarGenLangParser.GgrulebookContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="GrammarGenLangParser.ruledef"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterRuledef([NotNull] GrammarGenLangParser.RuledefContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="GrammarGenLangParser.ruledef"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitRuledef([NotNull] GrammarGenLangParser.RuledefContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="GrammarGenLangParser.ggrule"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterGgrule([NotNull] GrammarGenLangParser.GgruleContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="GrammarGenLangParser.ggrule"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitGgrule([NotNull] GrammarGenLangParser.GgruleContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="GrammarGenLangParser.paramrule"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterParamrule([NotNull] GrammarGenLangParser.ParamruleContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="GrammarGenLangParser.paramrule"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitParamrule([NotNull] GrammarGenLangParser.ParamruleContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Altern</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.ggproduction"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterAltern([NotNull] GrammarGenLangParser.AlternContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Altern</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.ggproduction"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitAltern([NotNull] GrammarGenLangParser.AlternContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Seq</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.alternative"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterSeq([NotNull] GrammarGenLangParser.SeqContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Seq</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.alternative"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitSeq([NotNull] GrammarGenLangParser.SeqContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="GrammarGenLangParser.condition"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterCondition([NotNull] GrammarGenLangParser.ConditionContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="GrammarGenLangParser.condition"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitCondition([NotNull] GrammarGenLangParser.ConditionContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="GrammarGenLangParser.negated"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterNegated([NotNull] GrammarGenLangParser.NegatedContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="GrammarGenLangParser.negated"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitNegated([NotNull] GrammarGenLangParser.NegatedContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="GrammarGenLangParser.weight"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterWeight([NotNull] GrammarGenLangParser.WeightContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="GrammarGenLangParser.weight"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitWeight([NotNull] GrammarGenLangParser.WeightContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Str</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterStr([NotNull] GrammarGenLangParser.StrContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Str</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitStr([NotNull] GrammarGenLangParser.StrContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Group</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterGroup([NotNull] GrammarGenLangParser.GroupContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Group</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitGroup([NotNull] GrammarGenLangParser.GroupContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Call</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterCall([NotNull] GrammarGenLangParser.CallContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Call</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitCall([NotNull] GrammarGenLangParser.CallContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Opt</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterOpt([NotNull] GrammarGenLangParser.OptContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Opt</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitOpt([NotNull] GrammarGenLangParser.OptContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Ident</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterIdent([NotNull] GrammarGenLangParser.IdentContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Ident</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitIdent([NotNull] GrammarGenLangParser.IdentContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Star</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterStar([NotNull] GrammarGenLangParser.StarContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Star</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitStar([NotNull] GrammarGenLangParser.StarContext context) { }
	/// <summary>
	/// Enter a parse tree produced by the <c>Plus</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterPlus([NotNull] GrammarGenLangParser.PlusContext context) { }
	/// <summary>
	/// Exit a parse tree produced by the <c>Plus</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitPlus([NotNull] GrammarGenLangParser.PlusContext context) { }

	/// <inheritdoc/>
	/// <remarks>The default implementation does nothing.</remarks>
	public virtual void EnterEveryRule([NotNull] ParserRuleContext context) { }
	/// <inheritdoc/>
	/// <remarks>The default implementation does nothing.</remarks>
	public virtual void ExitEveryRule([NotNull] ParserRuleContext context) { }
	/// <inheritdoc/>
	/// <remarks>The default implementation does nothing.</remarks>
	public virtual void VisitTerminal([NotNull] ITerminalNode node) { }
	/// <inheritdoc/>
	/// <remarks>The default implementation does nothing.</remarks>
	public virtual void VisitErrorNode([NotNull] IErrorNode node) { }
}
} // namespace SharpIrcBot.Plugins.GrammarGen.Lang
