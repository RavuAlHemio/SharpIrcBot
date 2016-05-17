using DatabaseNickMapping;
using Xunit;

namespace RegexTests
{
    public class DatabaseNickMappingTests
    {
        private static void TestLinkRegexValid(string testString)
        {
            var match = DatabaseNickMappingPlugin.LinkRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["baseNick"].Success);
            Assert.Equal("peszi_forum", match.Groups["baseNick"].Value);
            Assert.True(match.Groups["aliasNick"].Success);
            Assert.Equal("bowle", match.Groups["aliasNick"].Value);
        }

        private static void TestLinkRegexInvalid(string testString)
        {
            Assert.False(DatabaseNickMappingPlugin.LinkRegex.IsMatch(testString));
        }

        private static void TestUnlinkRegexValid(string testString)
        {
            var match = DatabaseNickMappingPlugin.UnlinkRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["nick"].Success);
            Assert.Equal("pf", match.Groups["nick"].Value);
        }

        private static void TestUnlinkRegexInvalid(string testString)
        {
            Assert.False(DatabaseNickMappingPlugin.UnlinkRegex.IsMatch(testString));
        }

        private static void TestBaseNickRegexValid(string testString)
        {
            var match = DatabaseNickMappingPlugin.BaseNickRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["nick"].Success);
            Assert.Equal("wvoulephiebolephie", match.Groups["nick"].Value);
        }

        private static void TestBaseNickRegexInvalid(string testString)
        {
            Assert.False(DatabaseNickMappingPlugin.BaseNickRegex.IsMatch(testString));
        }

        private static void TestPseudoRegisterRegexValid(string testString)
        {
            var match = DatabaseNickMappingPlugin.PseudoRegisterRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["nick"].Success);
            Assert.Equal("Chop", match.Groups["nick"].Value);
            Assert.False(match.Groups["unregister"].Success);
        }

        private static void TestPseudoUnregisterRegexValid(string testString)
        {
            var match = DatabaseNickMappingPlugin.PseudoRegisterRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["nick"].Success);
            Assert.Equal("Chop", match.Groups["nick"].Value);
            Assert.True(match.Groups["unregister"].Success);
        }

        private static void TestPseudoRegisterRegexInvalid(string testString)
        {
            Assert.False(DatabaseNickMappingPlugin.PseudoRegisterRegex.IsMatch(testString));
        }

        [Fact]
        public void TestLinkRegex()
        {
            // "!linknicks peszi_forum bowle"
            foreach (var testString in RegexTestUtils.SpaceOut("!linknicks", "peszi_forum", "bowle"))
            {
                TestLinkRegexValid(testString);
            }

            // wrong command: "!blep peszi_forum bowle"
            foreach (var testString in RegexTestUtils.SpaceOut("!blep", "peszi_forum", "bowle"))
            {
                TestLinkRegexInvalid(testString);
            }

            // no arguments: "!linknicks"
            foreach (var testString in RegexTestUtils.SpaceOut("!linknicks"))
            {
                TestLinkRegexInvalid(testString);
            }

            // too few arguments: "!linknicks peszi_forum"
            foreach (var testString in RegexTestUtils.SpaceOut("!linknicks", "peszi_forum"))
            {
                TestLinkRegexInvalid(testString);
            }

            // too many arguments: "!linknicks peszi_forum luna12 bowle"
            foreach (var testString in RegexTestUtils.SpaceOut("!linknicks", "peszi_forum", "bowle", "luna12"))
            {
                TestLinkRegexInvalid(testString);
            }

            // missing space: "!linknickspeszi_forum luna12"
            foreach (var testString in RegexTestUtils.SpaceOut("!linknickspeszi_forum", "bowle"))
            {
                TestLinkRegexInvalid(testString);
            }
        }

        [Fact]
        public void TestUnlinkRegex()
        {
            // "!unlinknick pf"
            foreach (var testString in RegexTestUtils.SpaceOut("!unlinknick", "pf"))
            {
                TestUnlinkRegexValid(testString);
            }

            // wrong command: "!blep pf"
            foreach (var testString in RegexTestUtils.SpaceOut("!blep", "pf"))
            {
                TestUnlinkRegexInvalid(testString);
            }

            // too few arguments: "!unlinknick"
            foreach (var testString in RegexTestUtils.SpaceOut("!unlinknick"))
            {
                TestUnlinkRegexInvalid(testString);
            }

            // too many arguments: "!unlinknick pf peszi_forum"
            foreach (var testString in RegexTestUtils.SpaceOut("!unlinknick", "pf", "peszi_forum"))
            {
                TestUnlinkRegexInvalid(testString);
            }

            // missing space: "!unlinknick pf peszi_forum"
            foreach (var testString in RegexTestUtils.SpaceOut("!unlinknickpf"))
            {
                TestUnlinkRegexInvalid(testString);
            }
        }

        [Fact]
        public void TestBaseNickRegex()
        {
            // "!basenick wvoulephiebolephie"
            foreach (var testString in RegexTestUtils.SpaceOut("!basenick", "wvoulephiebolephie"))
            {
                TestBaseNickRegexValid(testString);
            }

            // wrong command: "!blep wvoulephiebolephie"
            foreach (var testString in RegexTestUtils.SpaceOut("!blep", "wvoulephiebolephie"))
            {
                TestBaseNickRegexInvalid(testString);
            }

            // too few arguments: "!basenick"
            foreach (var testString in RegexTestUtils.SpaceOut("!basenick"))
            {
                TestBaseNickRegexInvalid(testString);
            }

            // too many arguments: "!basenick wvoulephiebolephie wolfibolfi"
            foreach (var testString in RegexTestUtils.SpaceOut("!basenick", "wvoulephiebolephie", "wolfibolfi"))
            {
                TestBaseNickRegexInvalid(testString);
            }

            // missing space: "!basenickwvoulephiebolephie"
            foreach (var testString in RegexTestUtils.SpaceOut("!basenickwvoulephiebolephie"))
            {
                TestBaseNickRegexInvalid(testString);
            }
        }

        [Fact]
        public void TestPseudoRegisterRegex()
        {
            // "!pseudoregister chop"
            foreach (var testString in RegexTestUtils.SpaceOut("!pseudoregister", "Chop"))
            {
                TestPseudoRegisterRegexValid(testString);
            }

            // "!pseudounregister chop"
            foreach (var testString in RegexTestUtils.SpaceOut("!pseudounregister", "Chop"))
            {
                TestPseudoUnregisterRegexValid(testString);
            }

            // no nickname: "!pseudoregister", "!pseudounregister"
            foreach (var testString in RegexTestUtils.SpaceOut("!pseudoregister"))
            {
                TestPseudoRegisterRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!pseudounregister"))
            {
                TestPseudoRegisterRegexInvalid(testString);
            }

            // too many nicknames: "!pseudoregister Chop Chop|mobile", "!pseudounregister Chop Chop|mobile"
            foreach (var testString in RegexTestUtils.SpaceOut("!pseudoregister", "Chop", "Chop|mobile"))
            {
                TestPseudoRegisterRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!pseudounregister", "Chop", "Chop|mobile"))
            {
                TestPseudoRegisterRegexInvalid(testString);
            }

            // no space: "!pseudoregisterChop", "!pseudounregisterChop"
            foreach (var testString in RegexTestUtils.SpaceOut("!pseudoregisterChop"))
            {
                TestPseudoRegisterRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!pseudounregisterChop"))
            {
                TestPseudoRegisterRegexInvalid(testString);
            }
        }
    }
}
