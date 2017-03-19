using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Sed
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SedConfig
    {
        public int RememberLastMessages { get; set; }

        public SedConfig(JObject obj)
        {
            RememberLastMessages = 50;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
