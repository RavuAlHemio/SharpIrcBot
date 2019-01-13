using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Vitals
{
    [JsonObject(MemberSerialization.OptOut)]
    public class VitalsConfig
    {
        public Dictionary<string, VitalsTarget> Targets { get; set; }
        public string DefaultTarget { get; set; }

        public double TimeoutSeconds { get; set; }

        public VitalsConfig(JObject obj)
        {
            Targets = new Dictionary<string, VitalsTarget>();
            TimeoutSeconds = 5.0;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
