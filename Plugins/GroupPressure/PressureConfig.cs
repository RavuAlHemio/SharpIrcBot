using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GroupPressure
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PressureConfig
    {
        public long BacklogSize { get; set; }
        public long TriggerCount { get; set; }
        public HashSet<string> Channels { get; set; }

        public PressureConfig(JObject obj)
        {
            BacklogSize = 20;
            TriggerCount = 3;
            Channels = new HashSet<string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
