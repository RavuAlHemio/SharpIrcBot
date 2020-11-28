using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.WordGen
{
    [JsonObject(MemberSerialization.OptOut)]
    public class WordGenConfig
    {
        public List<List<string>> Rings { get; set; }
        public double? MaxDurationSeconds { get; set; }

        public WordGenConfig(JObject obj)
        {
            Rings = new List<List<string>>();
            MaxDurationSeconds = 5;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
