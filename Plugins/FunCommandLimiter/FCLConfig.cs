using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.FunCommandLimiter
{
    [JsonObject(MemberSerialization.OptOut)]
    public class FCLConfig
    {
        public HashSet<string> CommandTags { get; set; }
        public double TimeSpanMinutes { get; set; }
        public int MaxCountPerTime { get; set; }
        public HashSet<string> Channels { get; set; }
        public bool PerUser { get; set; }
        public bool PerChannel { get; set; }
        public bool CountPrivateMessages { get; set; }
        public bool CountLimitedAttempts { get; set; }

        public FCLConfig(JObject obj)
        {
            CommandTags = new HashSet<string>();
            TimeSpanMinutes = 1440.0; // 1d
            MaxCountPerTime = 50;
            Channels = new HashSet<string>();
            PerUser = true;
            PerChannel = true;
            CountPrivateMessages = false;
            CountLimitedAttempts = true;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
