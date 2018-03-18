using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Debounce
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DebounceConfig
    {
        public List<BounceCriterion> Criteria { get; set; }

        public DebounceConfig(JObject obj)
        {
            Criteria = new List<BounceCriterion>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
