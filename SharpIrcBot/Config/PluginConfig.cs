using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Config
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PluginConfig
    {
        public bool Enabled { get; set; }
        [NotNull]
        public string Assembly { get; set; }
        [NotNull]
        public string Class { get; set; }
        [NotNull]
        public JObject Config { get; set; }

        public PluginConfig()
        {
            Enabled = true;
            Config = new JObject();
        }
    }
}
