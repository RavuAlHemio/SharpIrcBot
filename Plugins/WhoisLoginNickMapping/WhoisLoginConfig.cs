using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.WhoisLoginNickMapping
{
    [JsonObject(MemberSerialization.OptOut)]
    public class WhoisLoginConfig
    {
        public int LoginResponseCode { get; set; }
        public double ChannelSyncPeriodMinutes { get; set; }

        public WhoisLoginConfig(JObject obj)
        {
            LoginResponseCode = 330;
            ChannelSyncPeriodMinutes = 2.0;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
