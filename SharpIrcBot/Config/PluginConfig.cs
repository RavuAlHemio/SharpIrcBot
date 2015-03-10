using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PluginConfig
    {
        public string Assembly { get; set; }
        public string Class { get; set; }
        public JObject Config { get; set; }

        public PluginConfig()
        {
            Config = new JObject();
        }
    }
}

