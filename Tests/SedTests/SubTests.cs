using SharpIrcBot.Tests.TestPlumbing;
using SharpIrcBot.Tests.TestPlumbing.Events.Logging;
using Xunit;

namespace SharpIrcBot.Tests.SedTests
{
    public class SubTests
    {
        const string TestChannelName = "#test";

        [Fact]
        public void TestSingleReplacement()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s/d/fr/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("the quick brown fox jumps over the lazy frog", sentMessage.Body);
        }

        [Fact]
        public void TestSingleReplacementWithMultipleCandidates()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the_a_");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("a quick brown fox jumps over the lazy dog", sentMessage.Body);
        }

        [Fact]
        public void TestMultipleReplacement()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the_a_g");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("a quick brown fox jumps over a lazy dog", sentMessage.Body);
        }

        [Fact]
        public void TestLastButOneMatched()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what is this anyway?");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s_what_when_");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("when is this anyway?", sentMessage.Body);
        }

        [Fact]
        public void TestMatchOnlyMostRecent()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what is this anyway?");
            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what are we talking about?");
            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what does it mean?");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s_what_when_");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("when does it mean?", sentMessage.Body);
        }

        [Fact]
        public void TestEscapedSeparator()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "how and/or why?");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s/and\\/or/and\\/or\\/xor\\/nand\\/n\\or/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("how and/or/xor/nand/nor why?", sentMessage.Body);
        }

        [Fact]
        public void TestReplacementReference()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s_the_\\0 little_g");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("the little quick brown fox jumps over the little lazy dog", sentMessage.Body);
        }

        [Fact]
        public void TestReplacementReferenceToGroup()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "a one, a two, a three, a four");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s_(,)( a)_\\1 and\\2_g");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("a one, and a two, and a three, and a four", sentMessage.Body);
        }

        [Fact]
        public void TestReplaceFirstE()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(
                TestChannelName,
                "OneUser",
                "whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune"
            );
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s/e/3/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal(
                "wh3ther 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune",
                sentMessage.Body
            );
        }

        [Fact]
        public void TestReplaceFifthE()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(
                TestChannelName,
                "OneUser",
                "whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune"
            );
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s/e/3/5");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal(
                "whether 'tis nobler in the mind to suffer th3 slings and arrows of outrageous fortune",
                sentMessage.Body
            );
        }

        [Fact]
        public void TestReplaceEveryE()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(
                TestChannelName,
                "OneUser",
                "whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune"
            );
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s/e/3/g");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal(
                "wh3th3r 'tis nobl3r in th3 mind to suff3r th3 slings and arrows of outrag3ous fortun3",
                sentMessage.Body
            );
        }

        [Fact]
        public void TestReplaceFifthAndLaterE()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(
                TestChannelName,
                "OneUser",
                "whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune"
            );
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s/e/3/5g");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal(
                "whether 'tis nobler in the mind to suffer th3 slings and arrows of outrag3ous fortun3",
                sentMessage.Body
            );
        }

        [Fact]
        public void TestReplaceLastE()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(
                TestChannelName,
                "OneUser",
                "whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune"
            );
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s/e/3/-1");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal(
                "whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortun3",
                sentMessage.Body
            );
        }

        [Fact]
        public void TestReplaceLastButThreeE()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(
                TestChannelName,
                "OneUser",
                "whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune"
            );
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s/e/3/-4");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal(
                "whether 'tis nobler in the mind to suff3r the slings and arrows of outrageous fortune",
                sentMessage.Body
            );
        }

        [Fact]
        public void TestReplaceLastButThreeAndLaterE()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(
                TestChannelName,
                "OneUser",
                "whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune"
            );
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "s/e/3/-4g");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal(
                "whether 'tis nobler in the mind to suff3r th3 slings and arrows of outrag3ous fortun3",
                sentMessage.Body
            );
        }

        [Fact]
        public void TestMultiplePatterns()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "a one, a two, a three, a four");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", " s_e_o_g s/a/e/g  ");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("e ono, e two, e throo, e four", sentMessage.Body);
        }

        [Fact]
        public void TestTypo()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "i like the waether");
            mgr.InjectChannelMessage(TestChannelName, "OneUser", "s/ae/ea");
            mgr.InjectChannelMessage(TestChannelName, "OneUser", "s/ae/ea/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("i like the weather", sentMessage.Body);
        }

        [Fact]
        public void TestUnmatchedReplacement()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s+tiny++");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestInvalidSeparator()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s\\the\\a\\g");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestUnterminatedReplacement()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the_a");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestUnterminatedPattern()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestTooManySeparators()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the_a_g_");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestMixedFlagsDigits()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the_a_0g1");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestInterspersedMinusFlag()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the_a_0-1");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestIndexTooFar()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the_a_15");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestIndexTooEarly()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "s_the_a_-15");

            Assert.Equal(0, mgr.EventLog.Count);
        }
    }
}
