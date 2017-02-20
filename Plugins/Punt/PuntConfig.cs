using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Punt
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PuntConfig
    {
        public List<PuntPattern> CommonPatterns { get; set; }
        public Dictionary<string, List<PuntPattern>> ChannelsPatterns { get; set; }

        public PuntConfig(JObject obj)
        {
            CommonPatterns = new List<PuntPattern>();
            ChannelsPatterns = new Dictionary<string, List<PuntPattern>>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
