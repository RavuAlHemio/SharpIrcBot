using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using Quotes.ORM;
using SharpIrcBot;

namespace Quotes
{
    public class QuotesPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Regex RememberRegex = new Regex("^!remember[ ]+([^ ]+)[ ]+(.+)$");
        private static readonly Regex QuoteRegex = new Regex("^!quote(?:[ ]+([^ ]+))?$");

        protected ConnectionManager ConnectionManager;
        protected QuotesConfig Config;
        protected List<Quote> PotentialQuotes;
        protected Random Randomizer;

        public QuotesPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new QuotesConfig(config);
            PotentialQuotes = new List<Quote>(Config.RememberForQuotes + 1);
            Randomizer = new Random();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.ChannelAction += HandleChannelAction;
        }

        protected virtual void HandleChannelMessage(object sender, IrcEventArgs e)
        {
            var body = e.Data.Message;

            var rememberMatch = RememberRegex.Match(body);
            if (rememberMatch.Success)
            {
                var nick = rememberMatch.Groups[1].Value;
                var substring = rememberMatch.Groups[2].Value;

                var lowercaseNick = nick.ToLowerInvariant();
                var lowercaseSubstring = substring.ToLowerInvariant();

                if (lowercaseNick == e.Data.Nick.ToLowerInvariant())
                {
                    ConnectionManager.SendChannelMessageFormat(
                        e.Data.Channel,
                        "Sorry, {0}, someone else has to remember your quotes.",
                        e.Data.Nick
                    );
                    return;
                }

                // find it
                Quote matchedQuote = null;
                foreach (var potQuote in PotentialQuotes)
                {
                    if (potQuote.AuthorLowercase == lowercaseNick && potQuote.Body.ToLowerInvariant().Contains(lowercaseSubstring))
                    {
                        matchedQuote = potQuote;
                        break;
                    }
                }

                if (matchedQuote == null)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        e.Data.Channel,
                        "Sorry, {0}, I don't remember what {1} said about \"{2}\".",
                        e.Data.Nick,
                        nick,
                        substring
                    );
                    return;
                }

                using (var ctx = GetNewContext())
                {
                    ctx.Quotes.Add(matchedQuote);
                    ctx.SaveChanges();
                }

                ConnectionManager.SendChannelMessageFormat(
                    e.Data.Channel,
                    "Remembering {0}",
                    FormatQuote(matchedQuote)
                );

                return;
            }

            var quoteMatch = QuoteRegex.Match(body);
            if (quoteMatch.Success)
            {
                var nick = quoteMatch.Groups[1].Success ? quoteMatch.Groups[1].Value : null;
                var lowercaseNick = (nick != null) ? nick.ToLowerInvariant() : null;
                var found = false;

                using (var ctx = GetNewContext())
                {
                    var quotes = (nick == null)
                        ? ctx.Quotes
                        : ctx.Quotes.Where(q => q.AuthorLowercase == lowercaseNick);

                    int quoteCount = quotes.Count();
                    if (quoteCount > 0)
                    {
                        int index = Randomizer.Next(quoteCount);
                        var quote = quotes.OrderBy(q => q.ID).Skip(index).FirstOrDefault();
                        ConnectionManager.SendChannelMessage(
                            e.Data.Channel,
                            FormatQuote(quote)
                        );
                        found = true;
                    }
                }

                if (!found)
                {
                    if (nick == null)
                    {
                        ConnectionManager.SendChannelMessageFormat(
                            e.Data.Channel,
                            "Sorry, {0}, I don't have any quotes.",
                            e.Data.Nick
                        );
                    }
                    else
                    {
                        ConnectionManager.SendChannelMessageFormat(
                            e.Data.Channel,
                            "Sorry, {0}, I don't have any quotes for {1}.",
                            e.Data.Nick,
                            nick
                        );
                    }
                    return;
                }

                return;
            }

            // put into backlog
            var newQuote = new Quote
            {
                Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                Channel = e.Data.Channel,
                Author = e.Data.Nick,
                AuthorLowercase = e.Data.Nick.ToLowerInvariant(),
                MessageType = "M",
                Body = body
            };
            PotentialQuotes.Add(newQuote);

            CleanOutPotentialQuotes();
        }

        protected virtual void HandleChannelAction(object sender, ActionEventArgs e)
        {
            // put into backlog
            var quote = new Quote
            {
                Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                Channel = e.Data.Channel,
                Author = e.Data.Nick,
                AuthorLowercase = e.Data.Nick.ToLowerInvariant(),
                MessageType = "A",
                Body = e.ActionMessage
            };
            PotentialQuotes.Add(quote);

            CleanOutPotentialQuotes();
        }

        protected void CleanOutPotentialQuotes()
        {
            // clean out
            if (PotentialQuotes.Count > Config.RememberForQuotes)
            {
                PotentialQuotes.RemoveRange(0, PotentialQuotes.Count - Config.RememberForQuotes);
            }
        }

        protected string FormatQuote(Quote quote)
        {
            if (quote.MessageType == "M")
            {
                return string.Format("<{0}> {1}", quote.Author, quote.Body);
            }
            else if (quote.MessageType == "A")
            {
                return string.Format("* {0} {1}", quote.Author, quote.Body);
            }

            return null;
        }

        private QuotesContext GetNewContext()
        {
            var conn = SharpIrcBotUtil.GetDatabaseConnection(Config);
            return new QuotesContext(conn);
        }
    }
}
