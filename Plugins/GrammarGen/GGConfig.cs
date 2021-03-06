using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.GrammarGen
{
    [JsonObject(MemberSerialization.OptOut)]
    public class GGConfig
    {
        public string GrammarDir { get; set; }

        public GGConfig(JObject obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
