using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TextCommands
{
    [JsonObject(MemberSerialization.OptOut)]
    public class TextCommandsConfig
    {
        public Dictionary<string, List<string>> CommandsResponses { get; set; }
        public Dictionary<string, List<string>> NicknamableCommandsResponses { get; set; }

        public TextCommandsConfig(JObject obj)
        {
            CommandsResponses = new Dictionary<string, List<string>>();
            NicknamableCommandsResponses = new Dictionary<string, List<string>>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
