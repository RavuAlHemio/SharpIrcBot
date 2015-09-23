using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TextCommands
{
    [JsonObject(MemberSerialization.OptOut)]
    public class TextCommandsConfig
    {
        public Dictionary<string, string> CommandsResponses { get; set; }

        public TextCommandsConfig(JObject obj)
        {
            CommandsResponses = new Dictionary<string, string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
