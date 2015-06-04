using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace UnoBot
{
    [JsonObject(MemberSerialization.OptOut)]
    public class UnoBotConfig
    {
        public string UnoChannel { get; set; }

        public UnoBotConfig(JObject obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
