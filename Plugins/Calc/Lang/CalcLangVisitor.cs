//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from .\CalcLang.g4 by ANTLR 4.7.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace SharpIrcBot.Plugins.Calc.Language {
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="CalcLangParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7.1")]
[System.CLSCompliant(false)]
public interface ICalcLangVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="CalcLangParser.fullExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFullExpression([NotNull] CalcLangParser.FullExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Div</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDiv([NotNull] CalcLangParser.DivContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Add</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAdd([NotNull] CalcLangParser.AddContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Neg</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNeg([NotNull] CalcLangParser.NegContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Sub</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSub([NotNull] CalcLangParser.SubContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Dec</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDec([NotNull] CalcLangParser.DecContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Func</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunc([NotNull] CalcLangParser.FuncContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Cst</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCst([NotNull] CalcLangParser.CstContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Mul</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMul([NotNull] CalcLangParser.MulContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Parens</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParens([NotNull] CalcLangParser.ParensContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Pow</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPow([NotNull] CalcLangParser.PowContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Rem</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRem([NotNull] CalcLangParser.RemContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Int</c>
	/// labeled alternative in <see cref="CalcLangParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInt([NotNull] CalcLangParser.IntContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CalcLangParser.arglist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArglist([NotNull] CalcLangParser.ArglistContext context);
}
} // namespace SharpIrcBot.Plugins.Calc.Language
