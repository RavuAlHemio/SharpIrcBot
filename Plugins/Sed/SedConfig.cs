using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Sed
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SedConfig
    {
        public int RememberLastMessages { get; set; }
        public int MaxResultLength { get; set; }
        public string ResultTooLongMessage { get; set; }
        public HashSet<string> BannedUserRegexes { get; set; }

        public SedConfig(JObject obj)
        {
            RememberLastMessages = 50;
            MaxResultLength = 1024;
            ResultTooLongMessage = "(sorry, that's too long)";
            BannedUserRegexes = new HashSet<string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
