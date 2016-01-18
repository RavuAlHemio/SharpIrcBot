using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DontJustHighlightMe
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DJHMConfig
    {
        public HashSet<string> Channels { get; set; }

        public Dictionary<string, string> UserAliases { get; set; }

        public bool Kick { get; set; }

        public string KickMessage { get; set; }

        public int? TriggerPercentage { get; set; }

        public DJHMConfig(JObject obj)
        {
            Channels = new HashSet<string>();
            UserAliases = new Dictionary<string, string>();
            Kick = false;
            KickMessage = "Don't just highlight someone, tell them what you want!";
            TriggerPercentage = null;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
