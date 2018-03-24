using System.Collections.Generic;
using SharpIrcBot.Util;
using Xunit;

namespace SharpIrcBot.Tests.SharpIrcBotTests
{
    public class StringToCodePointStringsTests
    {
        static void StringTest(string str, params string[] codePoints)
        {
            Assert.Equal(
                (IEnumerable<string>)codePoints,
                (IEnumerable<string>)StringUtil.StringToCodePointStrings(str)
            );
        }

        [Fact]
        public void TestEmptyString()
        {
            StringTest("");
        }

        [Fact]
        public void TestBMPString()
        {
            StringTest("a\u12DC\u6AE6\u18A5\u2672", "a", "\u12DC", "\u6AE6", "\u18A5", "\u2672");
        }

        [Fact]
        public void TestLoneLeadingSurrogate()
        {
            StringTest("\uDADA", "\uDADA");
            StringTest("text\uDADA", "t", "e", "x", "t", "\uDADA");
            StringTest("\uDADAtest", "\uDADA", "t", "e", "s", "t");
            StringTest("text\uDADAtest", "t", "e", "x", "t", "\uDADA", "t", "e", "s", "t");
        }

        [Fact]
        public void TestLoneTrailingSurrogate()
        {
            StringTest("\uDEDE", "\uDEDE");
            StringTest("text\uDEDE", "t", "e", "x", "t", "\uDEDE");
            StringTest("\uDEDEtest", "\uDEDE", "t", "e", "s", "t");
            StringTest("text\uDEDEtest", "t", "e", "x", "t", "\uDEDE", "t", "e", "s", "t");
        }

        [Fact]
        public void TestValidSurrogatePair()
        {
            StringTest("\uD83D\uDE1B", "\U0001F61B");
            StringTest("\uD83D\uDE1B", "\uD83D\uDE1B");
            StringTest("text\uD83D\uDE1B", "t", "e", "x", "t", "\U0001F61B");
            StringTest("text\uD83D\uDE1B", "t", "e", "x", "t", "\uD83D\uDE1B");
            StringTest("\uD83D\uDE1Btest", "\U0001F61B", "t", "e", "s", "t");
            StringTest("\uD83D\uDE1Btest", "\uD83D\uDE1B", "t", "e", "s", "t");
            StringTest("text\uD83D\uDE1Btest", "t", "e", "x", "t", "\U0001F61B", "t", "e", "s", "t");
            StringTest("text\uD83D\uDE1Btest", "t", "e", "x", "t", "\uD83D\uDE1B", "t", "e", "s", "t");
        }

        [Fact]
        public void TestInvertedSurrogatePairs()
        {
            StringTest("\uDE1B\uD83D\uDE1B\uD83D", "\uDE1B", "\U0001F61B", "\uD83D");
        }
    }
}
