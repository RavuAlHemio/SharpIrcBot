using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Dereverse
{
    [JsonObject(MemberSerialization.OptOut)]
    public class WordListDereverseConfig
    {
        public HashSet<string> Channels { get; set; }
        public List<string> WordListFiles { get; set; }
        public decimal RatioThreshold { get; set; }

        public WordListDereverseConfig(JObject obj)
        {
            Channels = new HashSet<string>();
            WordListFiles = new List<string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
