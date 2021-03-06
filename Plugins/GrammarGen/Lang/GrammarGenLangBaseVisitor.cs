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
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;
using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;

/// <summary>
/// This class provides an empty implementation of <see cref="IGrammarGenLangVisitor{Result}"/>,
/// which can be extended to create a visitor which only needs to handle a subset
/// of the available methods.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.9.1")]
[System.Diagnostics.DebuggerNonUserCode]
[System.CLSCompliant(false)]
public partial class GrammarGenLangBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, IGrammarGenLangVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.ggrulebook"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitGgrulebook([NotNull] GrammarGenLangParser.GgrulebookContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.ruledef"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitRuledef([NotNull] GrammarGenLangParser.RuledefContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.ggrule"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitGgrule([NotNull] GrammarGenLangParser.GgruleContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.paramrule"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitParamrule([NotNull] GrammarGenLangParser.ParamruleContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Altern</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.ggproduction"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitAltern([NotNull] GrammarGenLangParser.AlternContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Seq</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.alternative"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitSeq([NotNull] GrammarGenLangParser.SeqContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Str</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitStr([NotNull] GrammarGenLangParser.StrContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Group</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitGroup([NotNull] GrammarGenLangParser.GroupContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Call</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitCall([NotNull] GrammarGenLangParser.CallContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Opt</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitOpt([NotNull] GrammarGenLangParser.OptContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Ident</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitIdent([NotNull] GrammarGenLangParser.IdentContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Star</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitStar([NotNull] GrammarGenLangParser.StarContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>Plus</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitPlus([NotNull] GrammarGenLangParser.PlusContext context) { return VisitChildren(context); }
}
} // namespace SharpIrcBot.Plugins.GrammarGen.Lang
