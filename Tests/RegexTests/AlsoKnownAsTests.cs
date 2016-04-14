using AlsoKnownAs;
using Xunit;

namespace RegexTests
{
    public class AlsoKnownAsTests
    {
        private static void TestAlsoKnownAsRegexValid(string testString)
        {
            var match = AlsoKnownAsPlugin.AlsoKnownAsRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["nickname"].Success);
            Assert.Equal("TheNickname", match.Groups["nickname"].Value);
        }

        private static void TestAlsoKnownAsRegexInvalid(string testString)
        {
            Assert.False(AlsoKnownAsPlugin.AlsoKnownAsRegex.IsMatch(testString));
        }

        [Fact]
        public void TestAlsoKnownAsRegex()
        {
            // "!aka TheNickname"
            foreach (var testString in RegexTestUtils.SpaceOut("!aka", "TheNickname"))
            {
                TestAlsoKnownAsRegexValid(testString);
            }

            // not !aka: "!othercmd TheNickname"
            foreach (var testString in RegexTestUtils.SpaceOut("!othercmd", "TheNickname"))
            {
                TestAlsoKnownAsRegexInvalid(testString);
            }

            // no nickname: "!aka"
            foreach (var testString in RegexTestUtils.SpaceOut("!aka"))
            {
                TestAlsoKnownAsRegexInvalid(testString);
            }

            // missing space: "!akaTheNickname"
            foreach (var testString in RegexTestUtils.SpaceOut("!akaTheNickname"))
            {
                TestAlsoKnownAsRegexInvalid(testString);
            }
        }
    }
}
