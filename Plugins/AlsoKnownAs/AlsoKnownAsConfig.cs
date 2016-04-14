using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlsoKnownAs
{
    [JsonObject(MemberSerialization.OptOut)]
    public class AlsoKnownAsConfig
    {
        [JsonIgnore]
        public Regex CloakedAddressRegex { get; set; }

        [JsonProperty("CloakedAddressRegex")]
        public string CloakedAddressRegexString
        {
            get { return CloakedAddressRegex.ToString(); }
            set { CloakedAddressRegex = new Regex(value, RegexOptions.Compiled); }
        }

        public AlsoKnownAsConfig(JObject obj)
        {
            CloakedAddressRegex = new Regex("^irc-([a-z0-9](?:\\.[a-z0-9])+)\\.IP$", RegexOptions.Compiled);

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
