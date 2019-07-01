using SharpIrcBot.Util;
using Xunit;

namespace SharpIrcBot.Tests.SharpIrcBotTests
{
    public class UnescapeStringTests
    {
        static void StringTest(string expected, string escapes)
        {
            Assert.Equal(
                (string)expected,
                (string)StringUtil.UnescapeString(escapes)
            );
        }

        [Fact]
        public void TestEmptyString()
        {
            StringTest("", "");
        }

        [Theory]
        [InlineData("foo"), InlineData("bar"), InlineData("baz")]
        [InlineData("jackdaws love my big sphinx of quartz")]
        [InlineData(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~")]
        public void TestNoEscapeString(string s)
        {
            StringTest(s, s);
        }

        [Theory]
        [InlineData("\\", "\\\\"), InlineData("\t", "\\t"), InlineData("\"", "\\\"")]
        [InlineData("\n", "\\n"), InlineData("\r", "\\r"), InlineData("'", "\\'")]
        [InlineData("\r\n", "\\r\\n")]
        [InlineData("one\ntwo", "one\\ntwo"), InlineData("one\r\ntwo", "one\\r\\ntwo")]
        [InlineData("one\ntwo", "one\\u000Atwo"), InlineData("one\r\ntwo", "one\\u000D\\u000Atwo")]
        [InlineData("\u00019876", "\\u00019876"), InlineData("\u00019876", "\\x019876")]
        [InlineData("\U0001D54F123", "\\U0001D54F123")]
        public void TestValidEscapeString(string expected, string escapes)
        {
            StringTest(expected, escapes);
        }

        [Theory]
        [InlineData("\\"), InlineData("test\\")]
        [InlineData("\\q")]
        public void TestInvalidEscapeString(string escapes)
        {
            StringTest(null, escapes);
        }
    }
}
