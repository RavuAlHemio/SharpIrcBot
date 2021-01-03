using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Config;

namespace SharpIrcBot.Plugins.Fact
{
    [JsonObject(MemberSerialization.OptOut)]
    public class FactConfig
    {
        public HashSet<string> Channels { get; set; }

        public Dictionary<string, List<PluginConfig>> CommandToSources { get; set; }

        public FactConfig(JObject obj)
        {
            Channels = new HashSet<string>();
            CommandToSources = new Dictionary<string, List<PluginConfig>>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
