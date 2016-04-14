using Allograph;
using Xunit;

namespace RegexTests
{
    public class AllographTests
    {
        private static void TestStatsRegexFullStatsValidChannel(string testString)
        {
            var match = AllographPlugin.StatsRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["channel"].Success);
            Assert.Equal("#chunnel", match.Groups["channel"].Value);
            Assert.False(match.Groups["testmsg"].Success);
        }

        private static void TestStatsRegexSingleWord(string testString)
        {
            var match = AllographPlugin.StatsRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["channel"].Success);
            Assert.Equal("#chunnel", match.Groups["channel"].Value);
            Assert.True(match.Groups["testmsg"].Success);
            Assert.Equal("tegeve", match.Groups["testmsg"].Value);
        }

        private static void TestStatsRegexMultiWord(string testString)
        {
            var match = AllographPlugin.StatsRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["channel"].Success);
            Assert.Equal("#chunnel", match.Groups["channel"].Value);
            Assert.True(match.Groups["testmsg"].Success);
            Assert.Equal("tegeve train a   grande   vitesse", match.Groups["testmsg"].Value);
        }

        private static void TestStatsRegexFail(string testString)
        {
            Assert.False(AllographPlugin.StatsRegex.IsMatch(testString));
        }

        [Fact]
        public void TestStatsRegex()
        {
            // full stats on valid channel
            TestStatsRegexFullStatsValidChannel("!allostats #chunnel");

            // whitespace strippage
            // "!allostats #chunnel"
            foreach (var testString in RegexTestUtils.SpaceOut("!allostats", "#chunnel"))
            {
                TestStatsRegexFullStatsValidChannel(testString);
            }

            // stats on single-word test string
            // "!allostats #chunnel tegeve"
            foreach (var testString in RegexTestUtils.SpaceOut("!allostats", "#chunnel", "tegeve"))
            {
                TestStatsRegexSingleWord(testString);
            }

            // stats on multi-word test string
            // "!allostats #chunnel tegeve train a   grande   vitesse"
            foreach (var testString in RegexTestUtils.SpaceOut("!allostats", "#chunnel", "tegeve train a   grande   vitesse"))
            {
                TestStatsRegexMultiWord(testString);
            }

            // completely different command
            TestStatsRegexFail("!woot");

            // no channel
            // "!allostats"
            foreach (var testString in RegexTestUtils.SpaceOut("!allostats"))
            {
                TestStatsRegexFail(testString);
            }

            // missing space
            // "!allostats#chunnel"
            foreach (var testString in RegexTestUtils.SpaceOut("!allostats#chunnel"))
            {
                TestStatsRegexFail(testString);
            }

            // wrong channel name
            // "!allostats !chunnel"
            foreach (var testString in RegexTestUtils.SpaceOut("!allostats", "!chunnel"))
            {
                TestStatsRegexFail(testString);
            }

            // wrong channel name despite test string
            // "!allostats !chunnel tegeve"
            foreach (var testString in RegexTestUtils.SpaceOut("!allostats", "!chunnel", "tegeve"))
            {
                TestStatsRegexFail(testString);
            }
        }
    }
}
