using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Quotes.ORM;

namespace SharpIrcBot.Plugins.Quotes
{
    public class QuotesPlugin : IPlugin, IReloadableConfiguration
    {
        public static readonly Regex AddQuoteRegex = new Regex("^!addquote\\s+(?<quote>\\S.*)$", RegexOptions.Compiled);
        public static readonly Regex RememberRegex = new Regex("^!remember\\s+(?<nick>\\S+)\\s+(?<pattern>\\S.*)$", RegexOptions.Compiled);
        public static readonly Regex QuoteRegex = new Regex("^!(?<rated>any|bad)?(?<showRating>r)?quote(?:\\s+(?<search>\\S.*)|\\s*)$", RegexOptions.Compiled);
        public static readonly Regex QuoteUserRegex = new Regex("^!(?<rated>any|bad)?(?<showRating>r)?quoteuser\\s+(?<username>\\S+)\\s*$", RegexOptions.Compiled);
        public static readonly Regex NextQuoteRegex = new Regex("^!next(?<rated>any|bad)?(?<showRating>r)?quote\\s*$", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager { get; }
        protected QuotesConfig Config { get; set; }
        protected Dictionary<string, List<Quote>> PotentialQuotesPerChannel { get; }
        protected Dictionary<string, long> LastQuoteIDs { get; }
        protected Random Randomizer { get; }

        protected List<Quote> ShuffledGoodQuotes { get; set; }
        protected List<Quote> ShuffledAnyQuotes { get; set; }
        protected List<Quote> ShuffledBadQuotes { get; set; }
        protected int ShuffledGoodQuotesIndex { get; set; }
        protected int ShuffledAnyQuotesIndex { get; set; }
        protected int ShuffledBadQuotesIndex { get; set; }

        public QuotesPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new QuotesConfig(config);
            PotentialQuotesPerChannel = new Dictionary<string, List<Quote>>();
            LastQuoteIDs = new Dictionary<string, long>();
            Randomizer = new Random();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.ChannelAction += HandleChannelAction;
            ConnectionManager.QueryMessage += HandleQueryMessage;
            ConnectionManager.BaseNickChanged += HandleBaseNickChanged;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new QuotesConfig(newConfig);

            // invalidate
            ShuffledAnyQuotes = null;
            ShuffledBadQuotes = null;
            ShuffledGoodQuotes = null;
        }

        protected virtual IQueryable<Quote> GetFilteredQuotes(IQueryable<Quote> quotesWithVotes, QuoteRating requestedRating)
        {
            switch (requestedRating)
            {
                case QuoteRating.Low:
                    return quotesWithVotes.Where(q => (q.Votes.Any() ? q.Votes.Sum(v => v.Points) : 0) < Config.VoteThreshold);
                case QuoteRating.Any:
                    return quotesWithVotes;
                case QuoteRating.High:
                    return quotesWithVotes.Where(q => (q.Votes.Any() ? q.Votes.Sum(v => v.Points) : 0) >= Config.VoteThreshold);
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
        /// <param name="addMyRating">If <c>true</c>, shows how the requestor voted on this quote.</param>
        /// <param name="postReply">Action to invoke to post a reply.</param>
        protected virtual void PostQuote(Quote quoteWithVotes, string requestor, string location, bool addMyRating, Action<string> postReply)
        {
            int voteCount = quoteWithVotes.Votes.Sum(v => (int?)v.Points) ?? 0;

            LastQuoteIDs[location] = quoteWithVotes.ID;
            if (addMyRating)
            {
                var requestorRegistered = ConnectionManager.RegisteredNameForNick(requestor) ?? requestor;
                var requestorLower = requestorRegistered.ToLowerInvariant();
                var requestorVote = quoteWithVotes.Votes.FirstOrDefault(v => v.VoterLowercase == requestorLower);

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

                postReply(FormatQuote(quoteWithVotes, voteCount, requestorVoteString));
            }
            else
            {
                postReply(FormatQuote(quoteWithVotes, voteCount, ""));
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
        protected virtual void PostRandomQuote(string requestor, string location, IQueryable<Quote> quotesWithVotes, QuoteRating requestedRating, bool addMyRating, Action<string> postReply)
        {
            var filteredQuotes = GetFilteredQuotes(quotesWithVotes, requestedRating);

            int quoteCount = filteredQuotes.Count();
            if (quoteCount > 0)
            {
                int index = Randomizer.Next(quoteCount);
                var quote = filteredQuotes
                    .OrderBy(q => q.ID)
                    .Skip(index)
                    .FirstOrDefault();

                PostQuote(quote, requestor, location, addMyRating, postReply);
            }
            else
            {
                postReply(string.Format("Sorry, {0}, I don't have any matching quotes.", requestor));
            }
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var body = e.Message;
            var normalizedNick = ConnectionManager.RegisteredNameForNick(e.SenderNickname) ?? e.SenderNickname;

            var addMatch = AddQuoteRegex.Match(body);
            if (addMatch.Success)
            {
                using (var ctx = GetNewContext())
                {
                    var newFreeFormQuote = new Quote
                    {
                        Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                        Channel = e.Channel,
                        Author = normalizedNick,
                        MessageType = "F",
                        Body = addMatch.Groups["quote"].Value
                    };
                    ctx.Quotes.Add(newFreeFormQuote);
                    ctx.SaveChanges();
                    LastQuoteIDs[e.Channel] = newFreeFormQuote.ID;
                }
                ConnectionManager.SendChannelMessage(
                    e.Channel,
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
                var nick = rememberMatch.Groups["nick"].Value;
                var substring = rememberMatch.Groups["pattern"].Value;

                var lowercaseSubstring = substring.ToLowerInvariant();

                var registeredNick = ConnectionManager.RegisteredNameForNick(nick) ?? nick;
                var lowercaseRegisteredNick = registeredNick.ToLowerInvariant();

                if (lowercaseRegisteredNick == normalizedNick.ToLowerInvariant())
                {
                    ConnectionManager.SendChannelMessageFormat(
                        e.Channel,
                        "Sorry, {0}, someone else has to remember your quotes.",
                        e.SenderNickname
                    );
                    return;
                }

                // find it
                var matchedQuote = PotentialQuotesPerChannel.ContainsKey(e.Channel)
                    ? PotentialQuotesPerChannel[e.Channel]
                        .OrderByDescending(potQuote => potQuote.Timestamp)
                        .FirstOrDefault(potQuote => potQuote.Author.ToLower() == lowercaseRegisteredNick && potQuote.Body.ToLower().Contains(lowercaseSubstring))
                    : null;

                if (matchedQuote == null)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        e.Channel,
                        "Sorry, {0}, I don't remember what {1} said about \"{2}\".",
                        e.SenderNickname,
                        nick,
                        substring
                    );
                    return;
                }

                using (var ctx = GetNewContext())
                {
                    ctx.Quotes.Add(matchedQuote);
                    ctx.SaveChanges();
                    LastQuoteIDs[e.Channel] = matchedQuote.ID;
                }

                ConnectionManager.SendChannelMessageFormat(
                    e.Channel,
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
                e.SenderNickname,
                e.Channel,
                e.Message,
                m => ConnectionManager.SendChannelMessage(e.Channel, m))
            )
            {
                // handled
                return;
            }

            // put into backlog
            var newQuote = new Quote
            {
                Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                Channel = e.Channel,
                Author = normalizedNick,
                MessageType = "M",
                Body = body
            };
            AddPotentialQuote(newQuote, e.Channel);

            CleanOutPotentialQuotes(e.Channel);
        }

        protected virtual void HandleChannelAction(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            // put into backlog
            var normalizedNick = ConnectionManager.RegisteredNameForNick(e.SenderNickname) ?? e.SenderNickname;
            var quote = new Quote
            {
                Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                Channel = e.Channel,
                Author = normalizedNick,
                MessageType = "A",
                Body = e.Message
            };
            AddPotentialQuote(quote, e.Channel);

            CleanOutPotentialQuotes(e.Channel);
        }

        protected virtual void HandleQueryMessage(object sender, IPrivateMessageEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (ActuallyHandleChannelOrQueryMessage(
                e.SenderNickname,
                e.SenderNickname,
                e.Message,
                m => ConnectionManager.SendQueryMessage(e.SenderNickname, m))
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
                var rating = QuoteRatingFromRegexGroup(quoteMatch.Groups["rated"]);
                bool addMyRating = quoteMatch.Groups["showRating"].Success;
                var subject = quoteMatch.Groups["search"].Success ? quoteMatch.Groups["search"].Value : null;
                var lowercaseSubject = subject?.ToLowerInvariant();

                using (var ctx = GetNewContext())
                {
                    IQueryable<Quote> quotes = (lowercaseSubject != null)
                        ? ctx.Quotes.Where(q => q.Body.ToLower().Contains(lowercaseSubject))
                        : ctx.Quotes;
                    IQueryable<Quote> quotesWithVotes = quotes.Include(q => q.Votes);

                    PostRandomQuote(sender, location, quotesWithVotes, rating, addMyRating, postReply);
                }

                return true;
            }

            var quoteUserMatch = QuoteUserRegex.Match(message);
            if (quoteUserMatch.Success)
            {
                var rating = QuoteRatingFromRegexGroup(quoteMatch.Groups["rated"]);
                bool addMyRating = quoteMatch.Groups["showRating"].Success;
                var nick = quoteMatch.Groups["username"].Value;
                var lowercaseNick = nick.ToLowerInvariant();

                using (var ctx = GetNewContext())
                {
                    IQueryable<Quote> quotesWithVotes = ctx.Quotes
                        .Include(q => q.Votes)
                        .Where(q => q.Author.ToLower() == lowercaseNick);

                    PostRandomQuote(sender, location, quotesWithVotes, rating, addMyRating, postReply);
                }

                return true;
            }

            var nextQuoteMatch = NextQuoteRegex.Match(message);
            if (nextQuoteMatch.Success)
            {
                var rating = QuoteRatingFromRegexGroup(nextQuoteMatch.Groups["rated"]);
                bool addMyRating = nextQuoteMatch.Groups["showRating"].Success;
                
                using (var ctx = GetNewContext())
                {
                    Quote quote = null;
                    switch (rating)
                    {
                        case QuoteRating.Any:
                            if (ShuffledAnyQuotes == null)
                            {
                                ShuffledAnyQuotes = GetFilteredQuotes(ctx.Quotes.Include(q => q.Votes), QuoteRating.Any)
                                    .ToShuffledList();
                                ShuffledAnyQuotesIndex = 0;
                            }
                            quote = ShuffledAnyQuotes[ShuffledAnyQuotesIndex++];
                            ShuffledAnyQuotesIndex %= ShuffledAnyQuotes.Count;
                            break;
                        case QuoteRating.High:
                            if (ShuffledGoodQuotes == null)
                            {
                                ShuffledGoodQuotes = GetFilteredQuotes(ctx.Quotes.Include(q => q.Votes), QuoteRating.High)
                                    .ToShuffledList();
                                ShuffledGoodQuotesIndex = 0;
                            }
                            quote = ShuffledGoodQuotes[ShuffledGoodQuotesIndex++];
                            ShuffledGoodQuotesIndex %= ShuffledGoodQuotes.Count;
                            break;
                        case QuoteRating.Low:
                            if (ShuffledBadQuotes == null)
                            {
                                ShuffledBadQuotes = GetFilteredQuotes(ctx.Quotes.Include(q => q.Votes), QuoteRating.Low)
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
                    
                    PostQuote(quote, sender, location, addMyRating, postReply);
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

        protected virtual void HandleBaseNickChanged(object sender, BaseNickChangedEventArgs e)
        {
            var oldNickLower = e.OldBaseNick.ToLowerInvariant();
            var newNickLower = e.NewBaseNick.ToLowerInvariant();

            using (var ctx = GetNewContext())
            {
                // fix up quote ownership
                var authorQuotes = ctx.Quotes
                    .Where(q => q.Author.ToLower() == oldNickLower);
                foreach (var authorQuote in authorQuotes)
                {
                    authorQuote.Author = e.NewBaseNick;
                }
                ctx.SaveChanges();

                // fix up quote votes
                var votes = ctx.QuoteVotes
                    .Where(qv => qv.VoterLowercase == oldNickLower || qv.VoterLowercase == newNickLower)
                    .ToList();
                foreach (var oldNickVote in votes.Where(qv => qv.VoterLowercase == oldNickLower))
                {
                    // did the new nick vote for the same quote?
                    if (votes.Any(qv => qv.QuoteID == oldNickVote.QuoteID && qv.VoterLowercase == newNickLower))
                    {
                        // yes; delete this one to prevent a duplicate
                        ctx.QuoteVotes.Remove(oldNickVote);
                    }
                    else
                    {
                        // no; update this one
                        oldNickVote.VoterLowercase = newNickLower;
                    }
                }
                ctx.SaveChanges();
            }
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
            var opts = SharpIrcBotUtil.GetContextOptions<QuotesContext>(Config);
            return new QuotesContext(opts);
        }
    }
}
