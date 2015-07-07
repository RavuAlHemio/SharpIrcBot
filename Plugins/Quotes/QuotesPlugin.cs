using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
        private static readonly Regex AddQuoteRegex = new Regex("^!addquote[ ]+(.+)$");
        private static readonly Regex RememberRegex = new Regex("^!remember[ ]+([^ ]+)[ ]+(.+)$");
        private static readonly Regex QuoteRegex = new Regex("^!quote(?:[ ]+(.+))?$");
        private static readonly Regex QuoteUserRegex = new Regex("^!quoteuser[ ]+([^ ]+)$");

        protected ConnectionManager ConnectionManager;
        protected QuotesConfig Config;
        protected List<Quote> PotentialQuotes;
        protected long LastQuoteID;
        protected Random Randomizer;

        public QuotesPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new QuotesConfig(config);
            PotentialQuotes = new List<Quote>(Config.RememberForQuotes + 1);
            LastQuoteID = -1;
            Randomizer = new Random();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.ChannelAction += HandleChannelAction;
        }

        protected virtual void PostRandomQuote(string requestor, string channel, IQueryable<Quote> quotes, IQueryable<QuoteVote> votes)
        {
            var qualityQuotes = quotes
                .Where(q => (votes.Where(v => v.QuoteID == q.ID).Sum(v => (int?)v.Points) ?? 0) >= Config.VoteThreshold);

            int quoteCount = qualityQuotes.Count();
            if (quoteCount > 0)
            {
                int index = Randomizer.Next(quoteCount);
                var quote = qualityQuotes
                    .OrderBy(q => q.ID)
                    .Skip(index)
                    .FirstOrDefault();
                LastQuoteID = quote.ID;
                ConnectionManager.SendChannelMessage(
                    channel,
                    FormatQuote(quote)
                );
            }
            else
            {
                ConnectionManager.SendChannelMessageFormat(
                    channel,
                    "Sorry, {0}, I don't have any matching quotes.",
                    requestor
                );
            }
        }

        protected void HandleChannelMessage(object sender, IrcEventArgs e)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, e);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling channel message", exc);
            }
        }

        protected void HandleChannelAction(object sender, ActionEventArgs e)
        {
            try
            {
                ActuallyHandleChannelAction(sender, e);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling channel action", exc);
            }
        }

        protected virtual void ActuallyHandleChannelMessage(object sender, IrcEventArgs e)
        {
            var body = e.Data.Message;

            var addMatch = AddQuoteRegex.Match(body);
            if (addMatch.Success)
            {
                using (var ctx = GetNewContext())
                {
                    var newFreeFormQuote = new Quote
                    {
                        Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                        Channel = e.Data.Channel,
                        Author = e.Data.Nick,
                        AuthorLowercase = e.Data.Nick.ToLowerInvariant(),
                        MessageType = "F",
                        Body = addMatch.Groups[1].Value,
                        BodyLowercase = addMatch.Groups[1].Value.ToLowerInvariant()
                    };
                    ctx.Quotes.Add(newFreeFormQuote);
                    ctx.SaveChanges();
                }
                ConnectionManager.SendChannelMessage(
                    e.Data.Channel,
                    "Done."
                );
                return;
            }

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
                    if (potQuote.AuthorLowercase == lowercaseNick && potQuote.BodyLowercase.Contains(lowercaseSubstring))
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
                var subject = quoteMatch.Groups[1].Success ? quoteMatch.Groups[1].Value : null;
                var lowercaseSubject = (subject != null) ? subject.ToLowerInvariant() : null;

                using (var ctx = GetNewContext())
                {
                    var quotes = (lowercaseSubject != null)
                        ? ctx.Quotes.Where(q => q.BodyLowercase.Contains(lowercaseSubject))
                        : ctx.Quotes;

                    PostRandomQuote(e.Data.Nick, e.Data.Channel, quotes, ctx.QuoteVotes);
                }

                return;
            }

            var quoteUserMatch = QuoteUserRegex.Match(body);
            if (quoteUserMatch.Success)
            {
                var nick = quoteMatch.Groups[1].Value;
                var lowercaseNick = nick.ToLowerInvariant();

                using (var ctx = GetNewContext())
                {
                    var quotes = ctx.Quotes.Where(q => q.AuthorLowercase == lowercaseNick);

                    PostRandomQuote(e.Data.Nick, e.Data.Channel, quotes, ctx.QuoteVotes);
                }

                return;
            }

            if (body == "!upquote" || body == "!uq")
            {
                if (LastQuoteID < 0)
                {
                    ConnectionManager.SendChannelMessage(
                        e.Data.Channel,
                        "You'll have to get a quote first..."
                    );
                    return;
                }
                UpsertVote(e.Data.Nick, LastQuoteID, 1);
                return;
            }

            if (body == "!downquote" || body == "!dq")
            {
                if (LastQuoteID < 0)
                {
                    ConnectionManager.SendChannelMessage(
                        e.Data.Channel,
                        "You'll have to get a quote first..."
                    );
                    return;
                }
                UpsertVote(e.Data.Nick, LastQuoteID, -1);
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
                Body = body,
                BodyLowercase = body.ToLowerInvariant()
            };
            PotentialQuotes.Add(newQuote);

            CleanOutPotentialQuotes();
        }

        protected virtual void ActuallyHandleChannelAction(object sender, ActionEventArgs e)
        {
            // put into backlog
            var quote = new Quote
            {
                Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                Channel = e.Data.Channel,
                Author = e.Data.Nick,
                AuthorLowercase = e.Data.Nick.ToLowerInvariant(),
                MessageType = "A",
                Body = e.ActionMessage,
                BodyLowercase = e.ActionMessage.ToLowerInvariant()
            };
            PotentialQuotes.Add(quote);

            CleanOutPotentialQuotes();
        }

        protected void UpsertVote(string voter, long quoteID, short points)
        {
            var voterLowercase = voter.ToLowerInvariant();
            using (var ctx = GetNewContext())
            {
                var vote = ctx.QuoteVotes.FirstOrDefault(v => v.QuoteID == quoteID && v.VoterLowercase == voterLowercase);
                if (vote == null)
                {
                    // add a new one
                    vote = new QuoteVote
                    {
                        QuoteID = quoteID,
                        VoterLowercase = voterLowercase,
                        Points = points
                    };
                    ctx.QuoteVotes.Add(vote);
                }
                else
                {
                    vote.Points = points;
                }
                ctx.SaveChanges();
            }
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
            else if (quote.MessageType == "F")
            {
                return quote.Body;
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
