using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.Allograph;
using SharpIrcBot.Plugins.LinkInfo;
using SharpIrcBot.Tests.TestPlumbing;
using SharpIrcBot.Tests.TestPlumbing.Events.Logging;
using Xunit;

namespace SharpIrcBot.Tests.AllographTests
{
    public class ReplaceFullMessageTests
    {
        [Fact]
        public void TestReplaceFullMessage()
        {
            const string constRegex = "(?i)^\\s*(?:wei(?:ss|sz|\u00DF)(?:\\s+hier)?\\s+(?:irgend)?(?:jemand|wer|einer?)|wei(?:ss|sz|\u00DF)t\\s+du|wisst\\s+ihr|wissen\\s+(?:sie|wir))\\s+.*(?:wer|von|ob|warum|wieso|weshalb|wo|was|wann|wessen|wie)\\s+.+$";
            const string constChannelName = "#test";
            const string constTrigger = "Wisst ihr was ich an http://ondrahosek.com am meisten hasse? ";
            const string constHumAFewBars = "Nein, aber wenn du ein paar Takte vorsummst kann ich so tun als ob.";

            var connMgr = new TestConnectionManager();
            var moduleConfig = new JObject
            {
                ["ProbabilityPercent"] = new JValue(100.0),
                ["Replacements"] = new JArray
                {
                    new JObject
                    {
                        ["RegexString"] = new JValue(constRegex),
                        ["ReplacementString"] = new JValue(constHumAFewBars),
                        ["ReplaceFullMessage"] = new JValue(true)
                    }
                }
            };

            // for chunk splitting
            var splitter = new LinkInfoPlugin(connMgr, new JObject());
            var allograph = new AllographPlugin(connMgr, moduleConfig);

            connMgr.InjectChannelMessage(
                constChannelName,
                "User",
                constTrigger
            );

            Assert.Equal(1, connMgr.EventLog.Count);
            var sentMessage = Assert.IsType<TestMessage>(connMgr.EventLog[0]);
            Assert.Equal(MessageType.Message, sentMessage.Type);
            Assert.Equal(constChannelName, sentMessage.Target);
            Assert.Equal(constHumAFewBars, sentMessage.Body);
        }
    }
}
