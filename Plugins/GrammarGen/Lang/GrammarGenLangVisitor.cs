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

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="GrammarGenLangParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.9.1")]
[System.CLSCompliant(false)]
public interface IGrammarGenLangVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.ggrulebook"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGgrulebook([NotNull] GrammarGenLangParser.GgrulebookContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.ruledef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRuledef([NotNull] GrammarGenLangParser.RuledefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.ggrule"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGgrule([NotNull] GrammarGenLangParser.GgruleContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.paramrule"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParamrule([NotNull] GrammarGenLangParser.ParamruleContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Altern</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.ggproduction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAltern([NotNull] GrammarGenLangParser.AlternContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Seq</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.alternative"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSeq([NotNull] GrammarGenLangParser.SeqContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.condition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCondition([NotNull] GrammarGenLangParser.ConditionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.negated"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNegated([NotNull] GrammarGenLangParser.NegatedContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="GrammarGenLangParser.weight"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitWeight([NotNull] GrammarGenLangParser.WeightContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Str</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStr([NotNull] GrammarGenLangParser.StrContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Group</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGroup([NotNull] GrammarGenLangParser.GroupContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Call</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCall([NotNull] GrammarGenLangParser.CallContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Opt</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOpt([NotNull] GrammarGenLangParser.OptContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Ident</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdent([NotNull] GrammarGenLangParser.IdentContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Star</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStar([NotNull] GrammarGenLangParser.StarContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Plus</c>
	/// labeled alternative in <see cref="GrammarGenLangParser.sequenceElem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPlus([NotNull] GrammarGenLangParser.PlusContext context);
}
} // namespace SharpIrcBot.Plugins.GrammarGen.Lang
