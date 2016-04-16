using LinkInfoOptIn;
using Xunit;

namespace RegexTests
{
    public class LinkInfoOptInTests
    {
        [Fact]
        public void TestAutoLinkInfoRegex()
        {
            // "!autolinkinfo"
            foreach (var testString in RegexTestUtils.SpaceOut("!autolinkinfo"))
            {
                var match = LinkInfoOptInPlugin.AutoLinkInfoRegex.Match(testString);
                Assert.True(match.Success);
                Assert.False(match.Groups["unsub"].Success);
            }

            // "!noautolinkinfo"
            foreach (var testString in RegexTestUtils.SpaceOut("!noautolinkinfo"))
            {
                var match = LinkInfoOptInPlugin.AutoLinkInfoRegex.Match(testString);
                Assert.True(match.Success);
                Assert.True(match.Groups["unsub"].Success);
            }

            // additional argument: "!autolinkinfo yes", "!noautolinkinfo yes"
            foreach (var testString in RegexTestUtils.SpaceOut("!autolinkinfo", "yes"))
            {
                Assert.False(LinkInfoOptInPlugin.AutoLinkInfoRegex.IsMatch(testString));
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!noautolinkinfo", "yes"))
            {
                Assert.False(LinkInfoOptInPlugin.AutoLinkInfoRegex.IsMatch(testString));
            }

            // wrong command: "!link http://www.google.com/"
            foreach (var testString in RegexTestUtils.SpaceOut("!link", "http://www.google.com/"))
            {
                Assert.False(LinkInfoOptInPlugin.AutoLinkInfoRegex.IsMatch(testString));
            }
        }
    }
}
