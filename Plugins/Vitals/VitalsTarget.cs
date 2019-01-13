using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Vitals
{
    [JsonObject(MemberSerialization.OptOut)]
    public class VitalsTarget
    {
        public string ReaderAssembly { get; set; } = null;
        public string ReaderClass { get; set; }
        public JObject ReaderOptions { get; set; }
    }
}
