using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Smileys
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SmileysConfig
    {
        public SortedSet<string> Smileys { get; set; }

        public SmileysConfig(JObject obj)
        {
            Smileys = new SortedSet<string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
