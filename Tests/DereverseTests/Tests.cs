using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.Dereverse;
using SharpIrcBot.Tests.TestPlumbing;
using SharpIrcBot.Tests.TestPlumbing.Events.Logging;
using Xunit;

namespace SharpIrcBot.Tests.DereverseTests
{
    public class Tests
    {
        const string TestChannelName = "#test";

        public static string Reversed(string s)
            => string.Concat(s.Reverse());

        public static TestConnectionManager ObtainConnectionManager()
        {
            var mgr = new TestConnectionManager();
            var dereverseConfig = new JObject
            {
                ["Channels"] = new JArray
                {
                    TestChannelName,
                },
                ["Scorers"] = new JArray
                {
                    new JObject
                    {
                        // original begins with "Ich "
                        ["Pattern"] = new JValue(" hcI$"),
                        ["ScoreAdjustment"] = new JValue(20.0m),
                    },
                    new JObject
                    {
                        // original ends with a period
                        ["Pattern"] = new JValue("^[.]"),
                        ["ScoreAdjustment"] = new JValue(5.0m),
                    },
                    new JObject
                    {
                        // original contains a word of one uppercase letter followed by at least three lowercase letters
                        ["Pattern"] = new JValue("\\b\\p{Ll}{3,}\\p{Lu}\\b"),
                        ["ScoreAdjustment"] = new JValue(5.0m),
                    },
                    new JObject
                    {
                        // original has a correct order of sign and space
                        ["Pattern"] = new JValue(" [.,?!][^.,?!]"),
                        ["ScoreAdjustment"] = new JValue(5.0m),
                    },
                },
                ["ScoreThreshold"] = new JValue(10.0m),
            };

            var sed = new DereversePlugin(mgr, dereverseConfig);
            return mgr;
        }

        [Theory]
        [InlineData("Ich beschreibe ein Fenster verspannt auf einem Bilderbuch, waehrend paulchen|mobil essend auf einem Buch fotografiert.")]
        [InlineData("Ich schlage das Handy redend an der Steckdose, zudem viperBOT bastelt.")]
        public void TestReversed(string messageToReverse)
        {
            TestConnectionManager mgr = ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "viperBOT", Reversed(messageToReverse));

            Assert.Equal(1, mgr.EventLog.Count);
            TestMessage sentMessage = Assert.IsType<TestMessage>(mgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(TestChannelName, sentMessage.Target);
            Assert.Equal(messageToReverse, sentMessage.Body);
        }

        [Theory]
        [InlineData(".rathole")]
        [InlineData(".asshole RavusBot")]
        [InlineData("jetzt muss ich wieder in den lockdown")]
        [InlineData("V-s oder so")]
        public void TestNotReversed(string unreversedMessage)
        {
            TestConnectionManager mgr = ObtainConnectionManager();

            mgr.InjectChannelMessage(TestChannelName, "viperBOT", unreversedMessage);

            Assert.Equal(0, mgr.EventLog.Count);
        }
    }
}
