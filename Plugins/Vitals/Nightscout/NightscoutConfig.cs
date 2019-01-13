using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Vitals.Nightscout
{
    [JsonObject(MemberSerialization.OptOut)]
    public class NightscoutConfig
    {
        public string Uri { get; set; }

        public NightscoutConfig(JObject obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
