using SharpIrcBot.Plugins.Messenger;
using Xunit;

namespace SharpIrcBot.Tests.RegexTests
{
    public class MessengerTests
    {
        private static void TestSendMessageRegexValid(string testString, bool silence)
        {
            var match = MessengerPlugin.SendMessageRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["silence"].Success);
            Assert.Equal(silence ? "s" : "", match.Groups["silence"].Value);
            Assert.True(match.Groups["recipient"].Success);
            Assert.Equal("ChanServ", match.Groups["recipient"].Value);
            Assert.True(match.Groups["message"].Success);
            Assert.Equal("!op me", match.Groups["message"].Value);
        }

        private static void TestSendMessageRegexInvalid(string testString)
        {
            Assert.False(MessengerPlugin.SendMessageRegex.IsMatch(testString));
        }

        private static void TestDeliverMessageRegexValid(string testString)
        {
            var match = MessengerPlugin.DeliverMessageRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["count"].Success);
            Assert.Equal("10", match.Groups["count"].Value);
        }

        private static void TestDeliverMessageRegexInvalid(string testString)
        {
            Assert.False(MessengerPlugin.DeliverMessageRegex.IsMatch(testString));
        }

        private static void TestReplayMessageRegexValid(string testString)
        {
            var match = MessengerPlugin.ReplayMessageRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["count"].Success);
            Assert.Equal("10", match.Groups["count"].Value);
        }

        private static void TestReplayMessageRegexInvalid(string testString)
        {
            Assert.False(MessengerPlugin.ReplayMessageRegex.IsMatch(testString));
        }

        private static void TestIgnoreMessageRegexValidIgnore(string testString)
        {
            var match = MessengerPlugin.IgnoreMessageRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["command"].Success);
            Assert.Equal("ignore", match.Groups["command"].Value);
            Assert.True(match.Groups["target"].Success);
            Assert.Equal("Schongo", match.Groups["target"].Value);
        }

        private static void TestIgnoreMessageRegexValidUnignore(string testString)
        {
            var match = MessengerPlugin.IgnoreMessageRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["command"].Success);
            Assert.Equal("unignore", match.Groups["command"].Value);
            Assert.True(match.Groups["target"].Success);
            Assert.Equal("Schongo", match.Groups["target"].Value);
        }

        private static void TestIgnoreMessageRegexInvalid(string testString)
        {
            Assert.False(MessengerPlugin.IgnoreMessageRegex.IsMatch(testString));
        }

        private static void TestQuiesceRegexValid(string testString)
        {
            var match = MessengerPlugin.QuiesceRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["messageCount"].Success);
            Assert.Equal("5", match.Groups["messageCount"].Value);
            Assert.True(match.Groups["durationHours"].Success);
            Assert.Equal("10", match.Groups["durationHours"].Value);
        }

        private static void TestQuiesceRegexInvalid(string testString)
        {
            Assert.False(MessengerPlugin.QuiesceRegex.IsMatch(testString));
        }

        private static void TestUnQuiesceRegexValid(string testString)
        {
            Assert.True(MessengerPlugin.UnQuiesceRegex.IsMatch(testString));
        }

        private static void TestUnQuiesceRegexInvalid(string testString)
        {
            Assert.False(MessengerPlugin.UnQuiesceRegex.IsMatch(testString));
        }
        private static void TestPrivateMessageRegexValid(string testString)
        {
            var match = MessengerPlugin.PrivateMessageRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["recipient"].Success);
            Assert.Equal("billygoat", match.Groups["recipient"].Value);
            Assert.True(match.Groups["message"].Success);
            Assert.Equal("I am watching you", match.Groups["message"].Value);
        }

        private static void TestPrivateMessageRegexInvalid(string testString)
        {
            Assert.False(MessengerPlugin.PrivateMessageRegex.IsMatch(testString));
        }

        [Fact]
        public void TestSendMessageRegex()
        {
            foreach (var cmd in new[] {"msg", "mail"})
            foreach (var silence in new[] {false, true})
            {
                string s = silence ? "s" : "";

                // missing everything: "!(s)?(msg|mail)"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + s + cmd))
                {
                    TestSendMessageRegexInvalid(testString);
                }

                foreach (var colon in new[] {"", ":"})
                {
                    // "!(s)?(msg|mail) ChanServ(:)? !op me"
                    foreach (var testString in RegexTestUtils.SpaceOut("!" + s + cmd, "ChanServ" + colon, "!op me"))
                    {
                        TestSendMessageRegexValid(testString, silence);
                    }

                    // missing message: "!(s)?(msg|mail) ChanServ(:)?"
                    foreach (var testString in RegexTestUtils.SpaceOut("!" + s + cmd, "ChanServ" + colon))
                    {
                        TestSendMessageRegexInvalid(testString);
                    }

                    // missing space: "!(s)?(msg|mail)ChanServ(:)? !op me"
                    foreach (var testString in RegexTestUtils.SpaceOut("!" + s + cmd + "ChanServ" + colon, "!op me"))
                    {
                        TestSendMessageRegexInvalid(testString);
                    }
                }
            }
        }

        [Fact]
        public void TestDeliverAndReplayMessageRegex()
        {
            foreach (var msgMail in new[] {"msg", "mail"})
            {
                // "!deliver(msg|mail) 10"
                foreach (var testString in RegexTestUtils.SpaceOut("!deliver" + msgMail, "10"))
                {
                    TestDeliverMessageRegexValid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!replay" + msgMail, "10"))
                {
                    TestReplayMessageRegexValid(testString);
                }

                // zero: "!deliver(msg|mail) 0"
                foreach (var testString in RegexTestUtils.SpaceOut("!deliver" + msgMail, "0"))
                {
                    TestDeliverMessageRegexInvalid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!replay" + msgMail, "0"))
                {
                    TestReplayMessageRegexInvalid(testString);
                }

                // negative: "!deliver(msg|mail) -13"
                foreach (var testString in RegexTestUtils.SpaceOut("!deliver" + msgMail, "-13"))
                {
                    TestDeliverMessageRegexInvalid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!replay" + msgMail, "-13"))
                {
                    TestReplayMessageRegexInvalid(testString);
                }

                // letters: "!deliver(msg|mail) ten"
                foreach (var testString in RegexTestUtils.SpaceOut("!deliver" + msgMail, "ten"))
                {
                    TestDeliverMessageRegexInvalid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!replay" + msgMail, "ten"))
                {
                    TestReplayMessageRegexInvalid(testString);
                }

                // supernumerary argument: "!deliver(msg|mail) 10 messages"
                foreach (var testString in RegexTestUtils.SpaceOut("!deliver" + msgMail, "10", "messages"))
                {
                    TestDeliverMessageRegexInvalid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!replay" + msgMail, "10", "messages"))
                {
                    TestReplayMessageRegexInvalid(testString);
                }

                // no space: "!deliver(msg|mail)10 messages"
                foreach (var testString in RegexTestUtils.SpaceOut("!deliver" + msgMail + "10", "messages"))
                {
                    TestDeliverMessageRegexInvalid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!replay" + msgMail + "10", "messages"))
                {
                    TestReplayMessageRegexInvalid(testString);
                }
            }
        }

        [Fact]
        public void TestIgnoreAndUnignoreMessageRegex()
        {
            foreach (var msgMail in new[] {"msg", "mail"})
            {
                // "!ignore(msg|mail) Schongo", "!unignore(msg|mail) Schongo"
                foreach (var testString in RegexTestUtils.SpaceOut("!ignore" + msgMail, "Schongo"))
                {
                    TestIgnoreMessageRegexValidIgnore(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!unignore" + msgMail, "Schongo"))
                {
                    TestIgnoreMessageRegexValidUnignore(testString);
                }

                // no nick: "!ignore(msg|mail)",  "!unignore(msg|mail)"
                foreach (var testString in RegexTestUtils.SpaceOut("!ignore" + msgMail))
                {
                    TestIgnoreMessageRegexInvalid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!unignore" + msgMail))
                {
                    TestIgnoreMessageRegexInvalid(testString);
                }

                // no space: "!ignore(msg|mail)Schongo", "!unignore(msg|mail)Schongo"
                foreach (var testString in RegexTestUtils.SpaceOut("!ignore" + msgMail + "Schongo"))
                {
                    TestIgnoreMessageRegexInvalid(testString);
                }
                foreach (var testString in RegexTestUtils.SpaceOut("!unignore" + msgMail + "Schongo"))
                {
                    TestIgnoreMessageRegexInvalid(testString);
                }
            }
        }

        [Fact]
        public void TestQuiesceAndUnQuiesceRegex()
        {
            foreach (var msgMail in new[] {"msg", "mail"})
            {
                // "!(msg|mail)gone 5 10h"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "gone", "5", "10h"))
                {
                    TestQuiesceRegexValid(testString);
                }

                // not enough arguments: "!(msg|mail)gone 5"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "gone", "5"))
                {
                    TestQuiesceRegexInvalid(testString);
                }

                // too many arguments: "!(msg|mail)gone 5 10h 15"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "gone", "5", "10h", "15"))
                {
                    TestQuiesceRegexInvalid(testString);
                }

                // no "h" after the second argument: "!(msg|mail)gone 5 10"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "gone", "5", "10"))
                {
                    TestQuiesceRegexInvalid(testString);
                }

                // wrong letter after the second argument: "!(msg|mail)gone 5 10k"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "gone", "5", "10k"))
                {
                    TestQuiesceRegexInvalid(testString);
                }

                // no space: "!(msg|mail)gone5 10h"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "gone5", "10h"))
                {
                    TestQuiesceRegexInvalid(testString);
                }

                // "!(msg|mail)back"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "back"))
                {
                    TestUnQuiesceRegexValid(testString);
                }

                // supernumerary argument: "!(msg|mail)back 12"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "back", "12"))
                {
                    TestUnQuiesceRegexInvalid(testString);
                }

                // two supernumerary arguments: "!(msg|mail)back 12 10h"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + msgMail + "back", "12", "10h"))
                {
                    TestUnQuiesceRegexInvalid(testString);
                }
            }
        }

        [Fact]
        public void TestPrivateMessageRegex()
        {
            foreach (var cmd in new[] { "pm", "pmsg", "pmail" })
            {
                // missing everything: "!(pm|pmsg|pmail)"
                foreach (var testString in RegexTestUtils.SpaceOut("!" + cmd))
                {
                    TestPrivateMessageRegexInvalid(testString);
                }

                foreach (var colon in new[] { "", ":" })
                {
                    // "!(pm|pmsg|pmail) billygoat(:)? I am watching you"
                    foreach (var testString in RegexTestUtils.SpaceOut("!" + cmd, "billygoat" + colon, "I am watching you"))
                    {
                        TestPrivateMessageRegexValid(testString);
                    }

                    // missing message: "!(pm|pmsg|pmail) billygoat(:)?"
                    foreach (var testString in RegexTestUtils.SpaceOut("!" + cmd, "billygoat" + colon))
                    {
                        TestPrivateMessageRegexInvalid(testString);
                    }

                    // missing space: "!(pm|pmsg|pmail)billygoat(:)? I am watching you"
                    foreach (var testString in RegexTestUtils.SpaceOut("!" + cmd + "billygoat" + colon, "I am watching you"))
                    {
                        TestSendMessageRegexInvalid(testString);
                    }
                }
            }
        }
    }
}
