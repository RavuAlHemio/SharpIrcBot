using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Fortune
{
    [JsonObject(MemberSerialization.OptOut)]
    public class FortuneConfig
    {
        public string FortuneDirectory { get; set; }

        public FortuneConfig(JObject obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
