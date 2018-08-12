using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Catchword
{
    [JsonObject(MemberSerialization.OptOut)]
    public class CatchwordConfig
    {
        [JsonObject(MemberSerialization.OptOut)]
        public class Replacement
        {
            public string RegexString
            {
                get { return Regex?.ToString(); }
                set { Regex = new Regex(value, RegexOptions.Compiled); }
            }

            [JsonIgnore]
            public Regex Regex { get; private set; }

            public string ReplacementString { get; set; }

            public decimal SkipChancePercent { get; set; }
        }

        public Dictionary<string, List<Replacement>> Catchments { get; set; }

        public CatchwordConfig(JObject obj)
        {
            Catchments = new Dictionary<string, List<Replacement>>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
