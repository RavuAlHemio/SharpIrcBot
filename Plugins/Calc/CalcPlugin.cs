using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Calc.AST;
using SharpIrcBot.Plugins.Calc.Language;

namespace SharpIrcBot.Plugins.Calc
{
    public class CalcPlugin : IPlugin
    {
        protected IConnectionManager ConnectionManager { get; }
        protected CalcConfig Config { get; set; }
        protected Dictionary<string, (string exprLine, string squiggleLine)> ChannelToLastFailure { get; set; }

        public CalcPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new CalcConfig(config);
            ChannelToLastFailure = new Dictionary<string, (string exprLine, string squiggleLine)>();

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("calc"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(RestTaker.Instance),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleCalcCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("calcwhere"),
                    CommandUtil.NoOptions,
                    CommandUtil.NoArguments,
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleCalcWhereCommand
            );
        }

        protected virtual void HandleCalcCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            var expression = (string)(cmd.Arguments[0]);
            string result;

            int position = -1;
            int length = -1;
            try
            {
                result = Calculate(expression);
            }
            catch (SimplificationException ex)
            {
                result = ex.Message;
                position = ex.Expression?.Index ?? -1;
                length = ex.Expression?.Length ?? -1;
            }

            if (Config.MaxResultStringLength > 0 && result.Length > Config.MaxResultStringLength)
            {
                result = "The result is too long to display.";
            }

            ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: {result}");
            if (position != -1 && length != -1)
            {
                // squiggly-underline the problematic section
                var underline = new StringBuilder();
                underline.Append(' ', position);
                underline.Append('~', length);

                ChannelToLastFailure[args.Channel] = (expression, underline.ToString());
            }
        }

        protected virtual void HandleCalcWhereCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            (string exprLine, string squiggleLine) linePair;
            if (!ChannelToLastFailure.TryGetValue(args.Channel, out linePair))
            {
                ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: Cannot remember a last error.");
            }

            ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: {linePair.exprLine}");
            ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: {linePair.squiggleLine}");
        }

        protected virtual string Calculate(string expression)
        {
            var timer = new CalcTimer(TimeSpan.FromSeconds(Config.TimeoutSeconds));
            return InternalCalculate(expression, timer);
        }

        public static Expression ParseExpression(string expression)
        {
            var charStream = new AntlrInputStream(expression);

            var lexer = new CalcLangLexer(charStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(PanicErrorListener.Instance);

            var tokenStream = new CommonTokenStream(lexer);

            var parser = new CalcLangParser(tokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(PanicErrorListener.Instance);

            CalcLangParser.ExpressionContext exprContext = parser.fullExpression().expression();
            var visitor = new ASTGrowingVisitor(tokenStream);
            return visitor.Visit(exprContext);
        }

        public static string InternalCalculate(string expression, CalcTimer timer)
        {
            Expression topExpression = ParseExpression(expression);
            Grimoire grimoire = Grimoire.CanonicalGrimoire;

            timer.Start();
            PrimitiveExpression simplifiedTop = topExpression.Simplified(grimoire, timer);

            return simplifiedTop.ToString();
        }
    }
}
