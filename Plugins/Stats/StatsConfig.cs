using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Stats
{
    [JsonObject(MemberSerialization.OptOut)]
    public class StatsConfig
    {
        public string DefaultTarget { get; set; }
        public string DistrictCoronaStatsUri { get; set; }
        public string DistrictPopFile { get; set; }
        public double? TimeoutSeconds { get; set; }

        public StatsConfig(JObject obj)
        {
            TimeoutSeconds = 5.0;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
