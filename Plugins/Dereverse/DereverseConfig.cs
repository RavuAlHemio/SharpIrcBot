using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Dereverse
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DereverseConfig
    {
        public HashSet<string> Channels { get; set; }
        public List<Scorer> Scorers { get; set; }
        public decimal ScoreThreshold { get; set; }

        public DereverseConfig(JObject obj)
        {
            Channels = new HashSet<string>();
            Scorers = new List<Scorer>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
