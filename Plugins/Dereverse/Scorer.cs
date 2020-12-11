using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Dereverse
{
    [JsonObject(MemberSerialization.OptOut)]
    public class Scorer
    {
        [JsonIgnore]
        public Regex Pattern { get; set; }

        public decimal ScoreAdjustment { get; set; }

        public bool KnockOut { get; set; }

        [JsonProperty("Pattern")]
        public string PatternString
        {
            get { return Pattern.ToString(); }
            set { Pattern = new Regex(value, RegexOptions.Compiled); }
        }
    }
}
