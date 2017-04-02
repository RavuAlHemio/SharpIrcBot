using SharpIrcBot.Tests.TestPlumbing;
using SharpIrcBot.Tests.TestPlumbing.Events.Logging;
using Xunit;

namespace SharpIrcBot.Tests.SedTests
{
    public class TransposeTests
    {
        const string TestChannelName = "#test";

        [Fact]
        public void TestStringTransposition()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr/aeiouy/431087/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("th3 q81ck br0wn f0x j8mps 0v3r th3 l4z7 d0g", sentMessage.Body);
        }

        [Fact]
        public void TestRangeTransposition()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr_a-z_A-Z_");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG", sentMessage.Body);
        }

        [Fact]
        public void TestOldRangeNewList()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr_a-d_ABCD_");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("the quiCk Brown fox jumps over the lAzy Dog", sentMessage.Body);
        }

        [Fact]
        public void TestOldListNewRange()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr_abcd_A-D_");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("the quiCk Brown fox jumps over the lAzy Dog", sentMessage.Body);
        }

        [Fact]
        public void TestRangeMix()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr_a-bc-d_A-D_");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("the quiCk Brown fox jumps over the lAzy Dog", sentMessage.Body);
        }

        [Fact]
        public void TestLastButOneMatched()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what is this anyway?");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "tr/q/t/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("the tuick brown fox jumps over the lazy dog", sentMessage.Body);
        }

        [Fact]
        public void TestMatchOnlyMostRecent()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what is this anyway?");
            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what are we talking about?");
            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what does it mean?");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "tr_wh_hw_");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("hwat does it mean?", sentMessage.Body);
        }

        [Fact]
        public void TestEscapedSeparator()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "how and/or why?");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", "tr/a\\/o/4_\\//");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("h/w 4nd_/r why?", sentMessage.Body);
        }

        [Fact]
        public void TestMultipleTranspositions()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "a one, a two, a three, a four");
            mgr.InjectChannelMessage(TestChannelName, "YetAnotherUser", " tr/a/e/ tr_o_u_  ");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("e une, e twu, e three, e fuur", sentMessage.Body);
        }

        [Fact]
        public void TestNonRangeHyphen1()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "twenty-three thousand four hundred fifty-six");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr/-a/|b/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("twenty|three thousbnd four hundred fifty|six", sentMessage.Body);
        }

        [Fact]
        public void TestNonRangeHyphen2()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "twenty-three thousand four hundred fifty-six");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr/a-d-/A-D|/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("twenty|three thousAnD four hunDreD fifty|six", sentMessage.Body);
        }

        [Fact]
        public void TestEmoji()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "lol X");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr/X/\U0001F600/");

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal("lol \U0001F600", sentMessage.Body);
        }

        [Fact]
        public void TestOverlongRange()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr/\u1000-\u2000/\u3000-\u4000/");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestInvertedRange()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr/z-a/Z-A/");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestUnmatchedTransposition()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "what is this i don't even");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr/q/m/");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestInvalidSeparator()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr\\the\\a\\");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestUnterminatedReplacement()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr_a_b");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestUnterminatedPattern()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr_the");

            Assert.Equal(0, mgr.EventLog.Count);
        }

        [Fact]
        public void TestTooManySeparators()
        {
            TestConnectionManager mgr = TestCommon.ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "OneUser", "the quick brown fox jumps over the lazy dog");
            mgr.InjectChannelMessage(TestChannelName, "AnotherUser", "tr_the_a_g_");

            Assert.Equal(0, mgr.EventLog.Count);
        }
    }
}
