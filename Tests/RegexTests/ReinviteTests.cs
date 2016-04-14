using Reinvite;
using Xunit;

namespace RegexTests
{
    public class ReinviteTests
    {
        private static void TestInviteRegexValid(string testString, string channel)
        {
            var match = ReinvitePlugin.InviteRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["channel"].Success);
            Assert.Equal(channel, match.Groups["channel"].Value);
        }

        private static void TestInviteRegexInvalid(string testString)
        {
            var match = ReinvitePlugin.InviteRegex.Match(testString);
            Assert.False(match.Success);
        }

        [Fact]
        public void TestInviteRegex()
        {
            foreach (var testString in RegexTestUtils.SpaceOut("!invite", "#chatbox"))
            {
                TestInviteRegexValid(testString, "#chatbox");
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!invite", "&chatbox"))
            {
                TestInviteRegexValid(testString, "&chatbox");
            }

            // wrong prefix sign
            foreach (var testString in RegexTestUtils.SpaceOut("!invite", "!chatbox"))
            {
                TestInviteRegexInvalid(testString);
            }

            // prefix sign only
            foreach (var testString in RegexTestUtils.SpaceOut("!invite", "#"))
            {
                TestInviteRegexInvalid(testString);
            }

            // supernumerary argument
            foreach (var testString in RegexTestUtils.SpaceOut("!invite", "#chatbox", "now"))
            {
                TestInviteRegexInvalid(testString);
            }

            // no space
            foreach (var testString in RegexTestUtils.SpaceOut("!invite#chatbox"))
            {
                TestInviteRegexInvalid(testString);
            }

            // channel name too long
            foreach (var testString in RegexTestUtils.SpaceOut("!invite", "#thebestchannelintheuniverseandalsotheotheruniversessobasicallythebestchannelinthemultiversebutihaventbeentoeveryplaceinthemultiverseyetsomaybeitisntthebestplaceinthemultiverseafterallwellihaventbeeneverywhereintheuniverseeithersoidontknowifmyopinioncanreallybetakenatfacevalue"))
            {
                TestInviteRegexInvalid(testString);
            }
        }
    }
}
