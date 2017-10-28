using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.DontJustHighlightMe
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DJHMConfig
    {
        public HashSet<string> Channels { get; set; }

        public Dictionary<string, string> UserAliases { get; set; }

        public HashSet<string> LowercaseImmuneNicksOrUsernames { get; set; }

        [JsonProperty("NonNicknameRegex")]
        public string NonNicknameRegexString
        {
            get { return NonNicknameRegex.ToString(); }
            set { NonNicknameRegex = new Regex(value, RegexOptions.Compiled); }
        }

        [JsonIgnore]
        public Regex NonNicknameRegex { get; set; }

        public bool Kick { get; set; }

        public string KickMessage { get; set; }

        public int? TriggerPercentage { get; set; }

        public int DelayMinMessages { get; set; }

        public int DelayMaxMessages { get; set; }

        public DJHMConfig(JObject obj)
        {
            Channels = new HashSet<string>();
            UserAliases = new Dictionary<string, string>();
            LowercaseImmuneNicksOrUsernames = new HashSet<string>();
            NonNicknameRegex = new Regex("[^a-zA-Z0-9_\\\\\\[\\]{}^`|-]+", RegexOptions.Compiled);
            Kick = false;
            KickMessage = "Don't just highlight someone, tell them what you want!";
            TriggerPercentage = null;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
