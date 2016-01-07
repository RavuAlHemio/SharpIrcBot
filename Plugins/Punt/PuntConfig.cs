using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Punt
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PuntConfig
    {
        public HashSet<PuntPattern> CommonPatterns { get; set; }
        public Dictionary<string, HashSet<PuntPattern>> ChannelsPatterns { get; set; }

        public PuntConfig(JObject obj)
        {
            CommonPatterns = new HashSet<PuntPattern>();
            ChannelsPatterns = new Dictionary<string, HashSet<PuntPattern>>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
