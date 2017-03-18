using SharpIrcBot.Plugins.Quotes;
using Xunit;

namespace SharpIrcBot.Tests.RegexTests
{
    public class QuotesTests
    {
        private static void TestRememberRegexValid(string testString)
        {
            var match = QuotesPlugin.RememberRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["nick"].Success);
            Assert.Equal("Romeo", match.Groups["nick"].Value);
            Assert.True(match.Groups["pattern"].Success);
            Assert.Equal("by any other name ", match.Groups["pattern"].Value);
        }

        private static void TestRememberRegexInvalid(string testString)
        {
            Assert.False(QuotesPlugin.RememberRegex.IsMatch(testString));
        }

        private static void TestQuoteRegexValid(string testString, string rated, bool showRating, string searchString)
        {
            var match = QuotesPlugin.QuoteRegex.Match(testString);
            Assert.True(match.Success);
            if (rated == null)
            {
                Assert.False(match.Groups["rated"].Success);
            }
            else
            {
                Assert.True(match.Groups["rated"].Success);
                Assert.Equal(rated, match.Groups["rated"].Value);
            }
            Assert.Equal(showRating, match.Groups["showRating"].Success);
            if (searchString == null)
            {
                Assert.False(match.Groups["search"].Success);
            }
            else
            {
                Assert.True(match.Groups["search"].Success);
                Assert.Equal(searchString, match.Groups["search"].Value);
            }
        }

        private static void TestQuoteRegexInvalid(string testString)
        {
            Assert.False(QuotesPlugin.QuoteRegex.IsMatch(testString));
        }

        private static void TestQuoteUserRegexValid(string testString, string rated, bool showRating, string username)
        {
            var match = QuotesPlugin.QuoteUserRegex.Match(testString);
            Assert.True(match.Success);
            if (rated == null)
            {
                Assert.False(match.Groups["rated"].Success);
            }
            else
            {
                Assert.True(match.Groups["rated"].Success);
                Assert.Equal(rated, match.Groups["rated"].Value);
            }
            Assert.Equal(showRating, match.Groups["showRating"].Success);
            Assert.True(match.Groups["username"].Success);
            Assert.Equal(username, match.Groups["username"].Value);
        }

        private static void TestQuoteUserRegexInvalid(string testString)
        {
            Assert.False(QuotesPlugin.QuoteUserRegex.IsMatch(testString));
        }

        private static void TestNextQuoteRegexValid(string testString, string rated, bool showRating)
        {
            var match = QuotesPlugin.NextQuoteRegex.Match(testString);
            Assert.True(match.Success);
            if (rated == null)
            {
                Assert.False(match.Groups["rated"].Success);
            }
            else
            {
                Assert.True(match.Groups["rated"].Success);
                Assert.Equal(rated, match.Groups["rated"].Value);
            }
            Assert.Equal(showRating, match.Groups["showRating"].Success);
        }

        private static void TestNextQuoteRegexInvalid(string testString)
        {
            Assert.False(QuotesPlugin.NextQuoteRegex.IsMatch(testString));
        }

        [Fact]
        public void TestAddQuoteRegex()
        {
            // "!addquote <ChanServ> Sorry, RavuAlHemio, that operation is not allowed."
            foreach (var testString in RegexTestUtils.SpaceOut("!addquote"))
            {
                var match = QuotesPlugin.AddQuoteRegex.Match(testString + " <ChanServ> Sorry, RavuAlHemio, that operation is not allowed.");
                Assert.True(match.Success);
                Assert.True(match.Groups["quote"].Success);
                Assert.Equal("<ChanServ> Sorry, RavuAlHemio, that operation is not allowed.", match.Groups["quote"].Value);
            }

            // no space: "!addquote<ChanServ> Sorry, RavuAlHemio, that operation is not allowed."
            foreach (var testString in RegexTestUtils.SpaceOut("!addquote<ChanServ> Sorry, RavuAlHemio, that operation is not allowed."))
            {
                Assert.False(QuotesPlugin.AddQuoteRegex.IsMatch(testString));
            }

            // no quote to add: "!addquote"
            foreach (var testString in RegexTestUtils.SpaceOut("!addquote"))
            {
                Assert.False(QuotesPlugin.AddQuoteRegex.IsMatch(testString));
            }
        }

        [Fact]
        public void TestRememberRegex()
        {
            // "!remember Romeo by any other name " (trailing space significant)
            foreach (var testString in RegexTestUtils.SpaceOut("!remember", "Romeo"))
            {
                TestRememberRegexValid(testString + " by any other name ");
            }

            // no phrase to remember: "!remember Romeo"
            foreach (var testString in RegexTestUtils.SpaceOut("!remember", "Romeo"))
            {
                TestRememberRegexInvalid(testString);
            }

            // no space: "!rememberRomeo by any other name "
            foreach (var testString in RegexTestUtils.SpaceOut("!rememberRomeo"))
            {
                TestRememberRegexInvalid(testString + " by any other name ");
            }

            // no arguments: "!remember"
            foreach (var testString in RegexTestUtils.SpaceOut("!remember"))
            {
                TestRememberRegexInvalid(testString);
            }
        }

        [Fact]
        public void TestQuoteRegex()
        {
            // "!quote", "!quote that would be an ecumenical matter"
            foreach (var testString in RegexTestUtils.SpaceOut("!quote"))
            {
                TestQuoteRegexValid(testString, null, false, null);
                TestQuoteRegexValid(testString + " that would be an ecumenical matter", null, false, "that would be an ecumenical matter");
            }
            // "!badquote", "!badquote that would be an ecumenical matter"
            foreach (var testString in RegexTestUtils.SpaceOut("!badquote"))
            {
                TestQuoteRegexValid(testString, "bad", false, null);
                TestQuoteRegexValid(testString + " that would be an ecumenical matter", "bad", false, "that would be an ecumenical matter");
            }
            // "!anyquote", "!anyquote that would be an ecumenical matter"
            foreach (var testString in RegexTestUtils.SpaceOut("!anyquote"))
            {
                TestQuoteRegexValid(testString, "any", false, null);
                TestQuoteRegexValid(testString + " that would be an ecumenical matter", "any", false, "that would be an ecumenical matter");
            }

            // "!rquote", "!rquote that would be an ecumenical matter"
            foreach (var testString in RegexTestUtils.SpaceOut("!rquote"))
            {
                TestQuoteRegexValid(testString, null, true, null);
                TestQuoteRegexValid(testString + " that would be an ecumenical matter", null, true, "that would be an ecumenical matter");
            }
            // "!badrquote", "!badrquote that would be an ecumenical matter"
            foreach (var testString in RegexTestUtils.SpaceOut("!badrquote"))
            {
                TestQuoteRegexValid(testString, "bad", true, null);
                TestQuoteRegexValid(testString + " that would be an ecumenical matter", "bad", true, "that would be an ecumenical matter");
            }
            // "!anyrquote", "!anyrquote that would be an ecumenical matter"
            foreach (var testString in RegexTestUtils.SpaceOut("!anyrquote"))
            {
                TestQuoteRegexValid(testString, "any", true, null);
                TestQuoteRegexValid(testString + " that would be an ecumenical matter", "any", true, "that would be an ecumenical matter");
            }

            // "good" is not a valid selector: "!goodquote", "!goodquote that would be an ecumenical matter"
            foreach (var testString in RegexTestUtils.SpaceOut("!goodquote"))
            {
                TestQuoteRegexInvalid(testString);
                TestQuoteRegexInvalid(testString + " that would be an ecumenical matter");
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!goodrquote"))
            {
                TestQuoteRegexInvalid(testString);
                TestQuoteRegexInvalid(testString + " that would be an ecumenical matter");
            }

            // completely wrong command: "!getline", "!getline that would be an ecumenical matter"
            foreach (var testString in RegexTestUtils.SpaceOut("!getline"))
            {
                TestQuoteRegexInvalid(testString);
                TestQuoteRegexInvalid(testString + " that would be an ecumenical matter");
            }

            // no space
            TestQuoteRegexInvalid("!quotethat would be an ecumenical matter");
            TestQuoteRegexInvalid("!badquotethat would be an ecumenical matter");
            TestQuoteRegexInvalid("!anyquotethat would be an ecumenical matter");
            TestQuoteRegexInvalid("!rquotethat would be an ecumenical matter");
            TestQuoteRegexInvalid("!badrquotethat would be an ecumenical matter");
            TestQuoteRegexInvalid("!anyrquotethat would be an ecumenical matter");
        }

        [Fact]
        public void TestQuoteUserRegex()
        {
            // "!quoteuser FatherJackHackett"
            foreach (var testString in RegexTestUtils.SpaceOut("!quoteuser", "FatherJackHackett"))
            {
                TestQuoteUserRegexValid(testString, null, false, "FatherJackHackett");
            }
            // "!badquoteuser FatherJackHackett"
            foreach (var testString in RegexTestUtils.SpaceOut("!badquoteuser", "FatherJackHackett"))
            {
                TestQuoteUserRegexValid(testString, "bad", false, "FatherJackHackett");
            }
            // "!anyquoteuser FatherJackHackett"
            foreach (var testString in RegexTestUtils.SpaceOut("!anyquoteuser", "FatherJackHackett"))
            {
                TestQuoteUserRegexValid(testString, "any", false, "FatherJackHackett");
            }

            // "!rquoteuser FatherJackHackett"
            foreach (var testString in RegexTestUtils.SpaceOut("!rquoteuser", "FatherJackHackett"))
            {
                TestQuoteUserRegexValid(testString, null, true, "FatherJackHackett");
            }
            // "!badrquoteuser FatherJackHackett"
            foreach (var testString in RegexTestUtils.SpaceOut("!badrquoteuser", "FatherJackHackett"))
            {
                TestQuoteUserRegexValid(testString, "bad", true, "FatherJackHackett");
            }
            // "!anyrquoteuser FatherJackHackett"
            foreach (var testString in RegexTestUtils.SpaceOut("!anyrquoteuser", "FatherJackHackett"))
            {
                TestQuoteUserRegexValid(testString, "any", true, "FatherJackHackett");
            }

            // "good" is not a valid selector: "!goodquoteuser FatherJackHackett", "!goodrquoteuser FatherJackHackett"
            foreach (var testString in RegexTestUtils.SpaceOut("!goodquoteuser", "FatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!goodrquoteuser", "FatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }

            // completely wrong command: "!getline FatherJackHackett"
            foreach (var testString in RegexTestUtils.SpaceOut("!getline", "FatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }

            // no space
            foreach (var testString in RegexTestUtils.SpaceOut("!quoteuserFatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!badquoteuserFatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!anyquoteuserFatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!rquoteuserFatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!badrquoteuserFatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!anyrquoteuserFatherJackHackett"))
            {
                TestQuoteUserRegexInvalid(testString);
            }

            // supernumerary argument: "!(|bad|any)(r)?quoteuser FatherJackHackett FatherTed"
            foreach (var command in new[] {"!quoteuser", "!badquoteuser", "!anyquoteuser", "!rquoteuser", "!rbadquoteuser", "!ranyquoteuser"})
            {
                foreach (var testString in RegexTestUtils.SpaceOut(command, "FatherJackHackett", "FatherTed"))
                {
                    TestQuoteUserRegexInvalid(testString);
                }
            }
        }

        [Fact]
        public void TestNextQuoteRegex()
        {
            foreach (var testString in RegexTestUtils.SpaceOut("!nextquote"))
            {
                TestNextQuoteRegexValid(testString, null, false);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!nextanyquote"))
            {
                TestNextQuoteRegexValid(testString, "any", false);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!nextbadquote"))
            {
                TestNextQuoteRegexValid(testString, "bad", false);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!nextrquote"))
            {
                TestNextQuoteRegexValid(testString, null, true);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!nextanyrquote"))
            {
                TestNextQuoteRegexValid(testString, "any", true);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!nextbadrquote"))
            {
                TestNextQuoteRegexValid(testString, "bad", true);
            }

            // "good" is not a valid selector
            foreach (var testString in RegexTestUtils.SpaceOut("!nextgoodquote"))
            {
                TestNextQuoteRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!nextgoodrquote"))
            {
                TestNextQuoteRegexInvalid(testString);
            }

            // completely wrong command
            foreach (var testString in RegexTestUtils.SpaceOut("!drink"))
            {
                TestNextQuoteRegexInvalid(testString);
            }
        }
    }
}
