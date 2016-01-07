using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Punt
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PuntConfig
    {
        public Dictionary<string, string> Patterns { get; set; }

        public PuntConfig(JObject obj)
        {
            Patterns = new Dictionary<string, string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
