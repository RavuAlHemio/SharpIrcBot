using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Sockpuppet
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SockpuppetConfig
    {
        public HashSet<string> Puppeteers { get; set; }
        public bool UseIrcServices { get; set; }

        public SockpuppetConfig(JObject obj)
        {
            Puppeteers = new HashSet<string>();
            UseIrcServices = true;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
