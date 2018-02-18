using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Counters
{
    [JsonObject(MemberSerialization.OptOut)]
    public class CountersConfig : IDatabaseModuleConfig
    {
        public string DatabaseProviderAssembly { get; set; }
        public string DatabaseConfiguratorClass { get; set; }
        public string DatabaseConfiguratorMethod { get; set; }
        public string DatabaseConnectionString { get; set; }

        public int BacklogSize { get; set; }
        public List<Counter> Counters { get; set; }
        public int TopCount { get; set; }
        public string TopCountText { get; set; }

        public CountersConfig(JObject obj)
        {
            BacklogSize = 50;
            Counters = new List<Counter>();
            TopCount = 5;
            TopCountText = "five";

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
