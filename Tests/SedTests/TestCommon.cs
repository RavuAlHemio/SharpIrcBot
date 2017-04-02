using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.Sed;
using SharpIrcBot.Tests.TestPlumbing;

namespace SharpIrcBot.Tests.SedTests
{
    public static class TestCommon
    {
        public static TestConnectionManager ObtainConnectionManager()
        {
            var mgr = new TestConnectionManager();
            var sedConfig = new JObject
            {
                ["RememberLastMessages"] = new JValue(50)
            };

            var sed = new SedPlugin(mgr, sedConfig);
            return mgr;
        }
    }
}
