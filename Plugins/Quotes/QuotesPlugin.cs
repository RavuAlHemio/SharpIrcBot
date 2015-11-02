using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly Regex AddQuoteRegex = new Regex("^!addquote[ ]+(.+)$");
        private static readonly Regex RememberRegex = new Regex("^!remember[ ]+([^ ]+)[ ]+(.+)$");
        private static readonly Regex QuoteRegex = new Regex("^!(any|bad)?(r)?quote(?:[ ]+(.+))?$");
        private static readonly Regex QuoteUserRegex = new Regex("^!(any|bad)?(r)?quoteuser[ ]+([^ ]+)$");
        private static readonly Regex NextQuoteRegex = new Regex("^!next(any|bad)?(r)?quote[ ]*$");

        protected ConnectionManager ConnectionManager;
        protected QuotesConfig Config;
        protected Dictionary<string, List<Quote>> PotentialQuotesPerChannel;
        protected Dictionary<string, long> LastQuoteIDs;
        protected Random Randomizer;

        protected List<Quote> ShuffledGoodQuotes { get; set; }
        protected List<Quote> ShuffledAnyQuotes { get; set; }
        protected List<Quote> ShuffledBadQuotes { get; set; }
        protected int ShuffledGoodQuotesIndex { get; set; }
        protected int ShuffledAnyQuotesIndex { get; set; }
        protected int ShuffledBadQuotesIndex { get; set; }

        public QuotesPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new QuotesConfig(config);
            PotentialQuotesPerChannel = new Dictionary<string, List<Quote>>();
            LastQuoteIDs = new Dictionary<string, long>();
            Randomizer = new Random();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.ChannelAction += HandleChannelAction;
            ConnectionManager.QueryMessage += HandleQueryMessage;
        }

        protected virtual IQueryable<Quote> GetFilteredQuotes(IQueryable<Quote> quotes, IQueryable<QuoteVote> votes, QuoteRating requestedRating)
        {
            switch (requestedRating)
            {
                case QuoteRating.Low:
                    return quotes.Where(q => (votes.Where(v => v.QuoteID == q.ID).Sum(v => (int?)v.Points) ?? 0) < Config.VoteThreshold);
                case QuoteRating.Any:
                    return quotes;
                case QuoteRating.High:
                    return quotes.Where(q => (votes.Where(v => v.QuoteID == q.ID).Sum(v => (int?)v.Points) ?? 0) >= Config.VoteThreshold);
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestedRating));
            }
        }

        /// <summary>
        /// Posts a specific quote.
        /// </summary>
        /// <param name="quote">The quote to post.</param>
        /// <param name="requestor">The nickname of the person who requested this quote.</param>
        /// <param name="location">The channel (for channel messages) or nickname (for private messages) in which the
        /// request has been placed.</param>
        /// <param name="votes">The Queryable of votes.</param>
        /// <param name="addMyRating">If <c>true</c>, shows how the requestor voted on this quote.</param>
        /// <param name="postReply">Action to invoke to post a reply.</param>
        protected virtual void PostQuote(Quote quote, string requestor, string location, IQueryable<QuoteVote> votes, bool addMyRating, Action<string> postReply)
        {
            int voteCount = votes.Where(v => v.QuoteID == quote.ID).Sum(v => (int?)v.Points) ?? 0;

            LastQuoteIDs[location] = quote.ID;
            if (addMyRating)
            {
                var requestorLower = requestor.ToLowerInvariant();
                var requestorVote = votes.FirstOrDefault(v => v.QuoteID == quote.ID && v.VoterLowercase == requestorLower);

                string requestorVoteString = " ";
                if (requestorVote != null)
                {
                    if (requestorVote.Points < 0)
                    {
                        requestorVoteString = "-";
                    }
                    else if (requestorVote.Points > 0)
                    {
                        requestorVoteString = "+";
                    }
                }

                postReply(FormatQuote(quote, voteCount, requestorVoteString));
            }
            else
            {
                postReply(FormatQuote(quote, voteCount, ""));
            }
        }

        /// <summary>
        /// Posts a random quote.
        /// </summary>
        /// <param name="requestor">The nickname of the person who requested this quote.</param>
        /// <param name="location">The channel (for channel messages) or nickname (for private messages) in which the
        /// request has been placed.</param>
        /// <param name="quotes">The Queryable of quotes.</param>
        /// <param name="votes">The Queryable of votes.</param>
        /// <param name="requestedRating">Whether to display all quotes, quotes rated equal to or above a certain threshold, or quotes rated below this threshold.</param>
        /// <param name="addMyRating">If <c>true</c>, shows how the requestor voted on this quote.</param>
        /// <param name="postReply">Action to invoke to post a reply.</param>
        protected virtual void PostRandomQuote(string requestor, string location, IQueryable<Quote> quotes, IQueryable<QuoteVote> votes, QuoteRating requestedRating, bool addMyRating, Action<string> postReply)
        {
            var filteredQuotes = GetFilteredQuotes(quotes, votes, requestedRating);

            int quoteCount = filteredQuotes.Count();
            if (quoteCount > 0)
            {
                int index = Randomizer.Next(quoteCount);
                var quote = filteredQuotes
                    .OrderBy(q => q.ID)
                    .Skip(index)
                    .FirstOrDefault();

                PostQuote(quote, requestor, location, votes, addMyRating, postReply);
            }
            else
            {
                postReply(string.Format("Sorry, {0}, I don't have any matching quotes.", requestor));
            }
        }

        protected void HandleChannelMessage(object sender, IrcEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, e, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling channel message", exc);
            }
        }

        protected void HandleChannelAction(object sender, ActionEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelAction(sender, e, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling channel action", exc);
            }
        }

        protected void HandleQueryMessage(object sender, IrcEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleQueryMessage(sender, e, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling query message", exc);
            }
        }

        protected virtual void ActuallyHandleChannelMessage(object sender, IrcEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var body = e.Data.Message;
            var normalizedNick = ConnectionManager.RegisteredNameForNick(e.Data.Nick) ?? e.Data.Nick;

            var addMatch = AddQuoteRegex.Match(body);
            if (addMatch.Success)
            {
                using (var ctx = GetNewContext())
                {
                    var newFreeFormQuote = new Quote
                    {
                        Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                        Channel = e.Data.Channel,
                        Author = normalizedNick,
                        AuthorLowercase = normalizedNick.ToLowerInvariant(),
                        MessageType = "F",
                        Body = addMatch.Groups[1].Value,
                        BodyLowercase = addMatch.Groups[1].Value.ToLowerInvariant()
                    };
                    ctx.Quotes.Add(newFreeFormQuote);
                    ctx.SaveChanges();
                    LastQuoteIDs[e.Data.Channel] = newFreeFormQuote.ID;
                }
                ConnectionManager.SendChannelMessage(
                    e.Data.Channel,
                    "Done."
                );

                // invalidate these
                ShuffledAnyQuotes = null;
                ShuffledBadQuotes = null;
                ShuffledGoodQuotes = null;

                return;
            }

            var rememberMatch = RememberRegex.Match(body);
            if (rememberMatch.Success)
            {
                var nick = rememberMatch.Groups[1].Value;
                var substring = rememberMatch.Groups[2].Value;

                var lowercaseNick = nick.ToLowerInvariant();
                var lowercaseSubstring = substring.ToLowerInvariant();

                if (lowercaseNick == normalizedNick.ToLowerInvariant())
                {
                    ConnectionManager.SendChannelMessageFormat(
                        e.Data.Channel,
                        "Sorry, {0}, someone else has to remember your quotes.",
                        e.Data.Nick
                    );
                    return;
                }

                // find it
                var matchedQuote = PotentialQuotesPerChannel.ContainsKey(e.Data.Channel)
                    ? PotentialQuotesPerChannel[e.Data.Channel].FirstOrDefault(potQuote => potQuote.AuthorLowercase == lowercaseNick && potQuote.BodyLowercase.Contains(lowercaseSubstring))
                    : null;

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
                    LastQuoteIDs[e.Data.Channel] = matchedQuote.ID;
                }

                ConnectionManager.SendChannelMessageFormat(
                    e.Data.Channel,
                    "Remembering {0}",
                    FormatQuote(matchedQuote, 0)
                );

                // invalidate these
                ShuffledAnyQuotes = null;
                ShuffledBadQuotes = null;
                ShuffledGoodQuotes = null;

                return;
            }

            if (ActuallyHandleChannelOrQueryMessage(
                e.Data.Nick,
                e.Data.Channel,
                e.Data.Message,
                m => ConnectionManager.SendChannelMessage(e.Data.Channel, m))
            )
            {
                // handled
                return;
            }

            // put into backlog
            var newQuote = new Quote
            {
                Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                Channel = e.Data.Channel,
                Author = normalizedNick,
                AuthorLowercase = normalizedNick.ToLowerInvariant(),
                MessageType = "M",
                Body = body,
                BodyLowercase = body.ToLowerInvariant()
            };
            AddPotentialQuote(newQuote, e.Data.Channel);

            CleanOutPotentialQuotes(e.Data.Channel);
        }

        protected virtual void ActuallyHandleChannelAction(object sender, ActionEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            // put into backlog
            var normalizedNick = ConnectionManager.RegisteredNameForNick(e.Data.Nick) ?? e.Data.Nick;
            var quote = new Quote
            {
                Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                Channel = e.Data.Channel,
                Author = normalizedNick,
                AuthorLowercase = normalizedNick.ToLowerInvariant(),
                MessageType = "A",
                Body = e.ActionMessage,
                BodyLowercase = e.ActionMessage.ToLowerInvariant()
            };
            AddPotentialQuote(quote, e.Data.Channel);

            CleanOutPotentialQuotes(e.Data.Channel);
        }

        protected virtual void ActuallyHandleQueryMessage(object sender, IrcEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (ActuallyHandleChannelOrQueryMessage(
                e.Data.Nick,
                e.Data.Nick,
                e.Data.Message,
                m => ConnectionManager.SendQueryMessage(e.Data.Nick, m))
            )
            {
                // handled
                return;
            }
        }

        private QuoteRating QuoteRatingFromRegexGroup(Group group)
        {
            if (!group.Success)
            {
                return QuoteRating.High;
            }

            if (group.Value == "any")
            {
                return QuoteRating.Any;
            }

            if (group.Value == "bad")
            {
                return QuoteRating.Low;
            }

            throw new ArgumentException("unknown group content", nameof(group));
        }

        protected virtual bool ActuallyHandleChannelOrQueryMessage(string sender, string location, string message, Action<string> postReply)
        {
            var normalizedSender = ConnectionManager.RegisteredNameForNick(sender) ?? sender;

            var quoteMatch = QuoteRegex.Match(message);
            if (quoteMatch.Success)
            {
                var rating = QuoteRatingFromRegexGroup(quoteMatch.Groups[1]);
                bool addMyRating = quoteMatch.Groups[2].Success;
                var subject = quoteMatch.Groups[3].Success ? quoteMatch.Groups[3].Value : null;
                var lowercaseSubject = (subject != null) ? subject.ToLowerInvariant() : null;

                using (var ctx = GetNewContext())
                {
                    var quotes = (lowercaseSubject != null)
                        ? ctx.Quotes.Where(q => q.BodyLowercase.Contains(lowercaseSubject))
                        : ctx.Quotes;

                    PostRandomQuote(sender, location, quotes, ctx.QuoteVotes, rating, addMyRating, postReply);
                }

                return true;
            }

            var quoteUserMatch = QuoteUserRegex.Match(message);
            if (quoteUserMatch.Success)
            {
                var rating = QuoteRatingFromRegexGroup(quoteMatch.Groups[1]);
                bool addMyRating = quoteMatch.Groups[2].Success;
                var nick = quoteMatch.Groups[3].Value;
                var lowercaseNick = nick.ToLowerInvariant();

                using (var ctx = GetNewContext())
                {
                    var quotes = ctx.Quotes.Where(q => q.AuthorLowercase == lowercaseNick);

                    PostRandomQuote(sender, location, quotes, ctx.QuoteVotes, rating, addMyRating, postReply);
                }

                return true;
            }

            var nextQuoteMatch = NextQuoteRegex.Match(message);
            if (nextQuoteMatch.Success)
            {
                var rating = QuoteRatingFromRegexGroup(quoteMatch.Groups[1]);
                bool addMyRating = quoteMatch.Groups[2].Success;
                
                using (var ctx = GetNewContext())
                {
                    Quote quote = null;
                    switch (rating)
                    {
                        case QuoteRating.Any:
                            if (ShuffledAnyQuotes == null)
                            {
                                ShuffledAnyQuotes = GetFilteredQuotes(ctx.Quotes, ctx.QuoteVotes, QuoteRating.Any)
                                    .ToShuffledList();
                                ShuffledAnyQuotesIndex = 0;
                            }
                            quote = ShuffledAnyQuotes[ShuffledAnyQuotesIndex++];
                            ShuffledAnyQuotesIndex %= ShuffledAnyQuotes.Count;
                            break;
                        case QuoteRating.High:
                            if (ShuffledGoodQuotes == null)
                            {
                                ShuffledGoodQuotes = GetFilteredQuotes(ctx.Quotes, ctx.QuoteVotes, QuoteRating.High)
                                    .ToShuffledList();
                                ShuffledGoodQuotesIndex = 0;
                            }
                            quote = ShuffledGoodQuotes[ShuffledGoodQuotesIndex++];
                            ShuffledGoodQuotesIndex %= ShuffledGoodQuotes.Count;
                            break;
                        case QuoteRating.Low:
                            if (ShuffledBadQuotes == null)
                            {
                                ShuffledBadQuotes = GetFilteredQuotes(ctx.Quotes, ctx.QuoteVotes, QuoteRating.Low)
                                    .ToShuffledList();
                                ShuffledBadQuotesIndex = 0;
                            }
                            quote = ShuffledBadQuotes[ShuffledBadQuotesIndex++];
                            ShuffledBadQuotesIndex %= ShuffledBadQuotes.Count;
                            break;
                        default:
                            Debug.Fail("unexpected quote rating");
                            break;
                    }
                    
                    PostQuote(quote, sender, location, ctx.QuoteVotes, addMyRating, postReply);
                }

                return true;
            }

            if (message == "!upquote" || message == "!uq")
            {
                if (!LastQuoteIDs.ContainsKey(location))
                {
                    postReply("You'll have to get a quote first...");
                    return true;
                }
                UpsertVote(normalizedSender, LastQuoteIDs[location], 1);
                return true;
            }

            if (message == "!downquote" || message == "!dq")
            {
                if (!LastQuoteIDs.ContainsKey(location))
                {
                    postReply("You'll have to get a quote first...");
                    return true;
                }
                UpsertVote(normalizedSender, LastQuoteIDs[location], -1);
                return true;
            }

            return false;
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

        protected void AddPotentialQuote(Quote quote, string channel)
        {
            if (!PotentialQuotesPerChannel.ContainsKey(channel))
            {
                PotentialQuotesPerChannel[channel] = new List<Quote>(Config.RememberForQuotes + 1);
            }
            PotentialQuotesPerChannel[channel].Add(quote);
        }

        protected void CleanOutPotentialQuotes(string channel)
        {
            // clean out
            if (!PotentialQuotesPerChannel.ContainsKey(channel))
            {
                return;
            }

            var thisChannelPotentialQuotes = PotentialQuotesPerChannel[channel];
            if (thisChannelPotentialQuotes.Count > Config.RememberForQuotes)
            {
                thisChannelPotentialQuotes.RemoveRange(0, thisChannelPotentialQuotes.Count - Config.RememberForQuotes);
            }
        }

        protected string FormatQuote(Quote quote, int voteCount, string requestorVote = "")
        {
            if (quote.MessageType == "M")
            {
                return string.Format("[{0}{3}] <{1}> {2}", voteCount, quote.Author, quote.Body, requestorVote);
            }
            else if (quote.MessageType == "A")
            {
                return string.Format("[{0}{3}] * {1} {2}", voteCount, quote.Author, quote.Body, requestorVote);
            }
            else if (quote.MessageType == "F")
            {
                return string.Format("[{0}{2}] {1}", voteCount, quote.Body, requestorVote);
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
