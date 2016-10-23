using WhoDoYouThinkIs;
using Xunit;

namespace RegexTests
{
    public class WhoDoYouThinkIsTests
    {
        private static void TestWdytiRegexValid(string testString, string nick)
        {
            var match = WhoDoYouThinkIsPlugin.WhoDoYouThinkIsRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["nick"].Success);
            Assert.Equal(nick, match.Groups["nick"].Value);
        }

        private static void TestWdytiRegexInvalid(string testString)
        {
            Assert.False(WhoDoYouThinkIsPlugin.WhoDoYouThinkIsRegex.IsMatch(testString));
        }

        [Fact]
        public void TestWdytiRegex()
        {
            foreach (var testString in RegexTestUtils.SpaceOut("!wdyti", "RavuAlHemio"))
            {
                TestWdytiRegexValid(testString, "RavuAlHemio");
            }

            // missing argument
            foreach (var testString in RegexTestUtils.SpaceOut("!wdyti"))
            {
                TestWdytiRegexInvalid(testString);
            }

            // supernumerary argument
            foreach (var testString in RegexTestUtils.SpaceOut("!wdyti", "RavuAlHemio", "RavusBot"))
            {
                TestWdytiRegexInvalid(testString);
            }

            // unspaced argument
            foreach (var testString in RegexTestUtils.SpaceOut("!wdytiRavuAlHemio"))
            {
                TestWdytiRegexInvalid(testString);
            }
        }
    }
}
