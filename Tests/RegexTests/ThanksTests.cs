using SharpIrcBot.Plugins.Thanks;
using Xunit;

namespace SharpIrcBot.Tests.RegexTests
{
    public class ThanksTests
    {
        private static void TestThankRegexValid(string testString, bool force, string reason)
        {
            var match = ThanksPlugin.ThankRegex.Match(testString);
            Assert.True(match.Success);
            Assert.Equal(force, match.Groups["force"].Success);
            Assert.True(match.Groups["thankee"].Success);
            Assert.Equal("libexec", match.Groups["thankee"].Value);
            if (reason == null)
            {
                Assert.False(match.Groups["reason"].Success);
            }
            else
            {
                Assert.True(match.Groups["reason"].Success);
                Assert.Equal(reason, match.Groups["reason"].Value);
            }
        }

        private static void TestThankRegexInvalid(string testString)
        {
            var match = ThanksPlugin.ThankRegex.Match(testString);
            Assert.False(match.Success);
        }

        private static void TestThankedRegexValid(string testString, bool raw)
        {
            var match = ThanksPlugin.ThankedRegex.Match(testString);
            Assert.True(match.Success);
            Assert.Equal(raw, match.Groups["raw"].Success);
            Assert.True(match.Groups["thankee"].Success);
            Assert.Equal("libexec", match.Groups["thankee"].Value);
        }

        private static void TestThankedRegexInvalid(string testString)
        {
            var match = ThanksPlugin.ThankedRegex.Match(testString);
            Assert.False(match.Success);
        }

        [Fact]
        public void TestThankRegex()
        {
            foreach (var thanks in new[] {"!thank", "!thanks", "!thx"})
            {
                foreach (var testString in RegexTestUtils.SpaceOut(thanks, "libexec"))
                {
                    TestThankRegexValid(testString, false, null);
                }
                foreach (var testString in RegexTestUtils.SpaceOut(thanks, "--force", "libexec"))
                {
                    TestThankRegexValid(testString, true, null);
                }

                foreach (var testString in RegexTestUtils.SpaceOut(thanks, "libexec", "kicking the spammer"))
                {
                    TestThankRegexValid(testString, false, "kicking the spammer");
                }
                foreach (var testString in RegexTestUtils.SpaceOut(thanks, "--force", "libexec", "kicking the spammer"))
                {
                    TestThankRegexValid(testString, true, "kicking the spammer");
                }

                // option --farce does not exist
                foreach (var testString in RegexTestUtils.SpaceOut(thanks, "--farce", "libexec"))
                {
                    TestThankRegexInvalid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut(thanks, "--farce", "libexec", "kicking the spammer"))
                {
                    TestThankRegexInvalid(testString);
                }

                // --force without a nickname
                foreach (var testString in RegexTestUtils.SpaceOut(thanks, "--force"))
                {
                    TestThankRegexInvalid(testString);
                }

                // !thanks without anything else
                foreach (var testString in RegexTestUtils.SpaceOut(thanks))
                {
                    TestThankRegexInvalid(testString);
                }
            }
        }

        [Fact]
        public void TestThankedRegex()
        {
            foreach (var testString in RegexTestUtils.SpaceOut("!thanked", "libexec"))
            {
                TestThankedRegexValid(testString, false);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!thanked", "--raw", "libexec"))
            {
                TestThankedRegexValid(testString, true);
            }

            // option --row does not exist
            foreach (var testString in RegexTestUtils.SpaceOut("!thanked", "--row", "libexec"))
            {
                TestThankedRegexInvalid(testString);
            }

            // --raw without a nickname
            foreach (var testString in RegexTestUtils.SpaceOut("!thanked", "--raw"))
            {
                TestThankedRegexInvalid(testString);
            }

            // no space
            foreach (var testString in RegexTestUtils.SpaceOut("!thankedlibexec"))
            {
                TestThankedRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!thanked--raw", "libexec"))
            {
                TestThankedRegexInvalid(testString);
            }

            // !thanked without anything else
            foreach (var testString in RegexTestUtils.SpaceOut("!thanked"))
            {
                TestThankedRegexInvalid(testString);
            }
        }
    }
}
