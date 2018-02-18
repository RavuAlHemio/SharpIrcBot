using System;
using Antlr4.Runtime;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Calc.AST;
using SharpIrcBot.Plugins.Calc.Language;

namespace SharpIrcBot.Plugins.Calc
{
    public class CalcPlugin : IPlugin
    {
        protected IConnectionManager ConnectionManager;

        public CalcPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("calc"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(RestTaker.Instance),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleCalcCommand
            );
        }

        protected virtual void HandleCalcCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            var expression = (string)(cmd.Arguments[0]);
            string result;

            try
            {
                result = Calculate(expression);
            }
            catch (OverflowException)
            {
                result = "Overflow.";
            }
            catch (DivideByZeroException)
            {
                result = "Division by zero.";
            }
            catch (FunctionDomainException)
            {
                result = "Undefined value.";
            }
            catch (SimplificationException ex)
            {
                result = ex.Message;
            }

            ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: {result}");
        }

        protected virtual string Calculate(string expression)
        {
            var charStream = new AntlrInputStream(expression);

            var lexer = new CalcLangLexer(charStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(PanicErrorListener.Instance);

            var tokenStream = new CommonTokenStream(lexer);

            var parser = new CalcLangParser(tokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(PanicErrorListener.Instance);

            CalcLangParser.ExpressionContext exprContext = parser.expression();
            var visitor = new ASTGrowingVisitor();
            Expression topExpression = visitor.Visit(exprContext);

            Grimoire grimoire = Grimoire.CanonicalGrimoire;

            PrimitiveExpression simplifiedTop = topExpression.Simplified(grimoire);

            return simplifiedTop.ToString();
        }
    }
}
