using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Fortune
{
    [JsonObject(MemberSerialization.OptOut)]
    public class FortuneConfig
    {
        public string FortuneDirectory { get; set; }
        public HashSet<string> AllowedCategories { get; set; }
        public int? MaxChars { get; set; }
        public int? MaxLines { get; set; }
        public int? MaxIntersperseCount { get; set; }

        public FortuneConfig(JObject obj)
        {
            MaxChars = null;
            MaxLines = null;
            MaxIntersperseCount = null;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
