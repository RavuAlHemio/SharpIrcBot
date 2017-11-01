using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Counters
{
    [JsonObject(MemberSerialization.OptOut)]
    public class Counter
    {
        public string Name { get; set; }

        public string CommandName { get; set; }

        [JsonIgnore]
        [CanBeNull]
        public Regex NicknameRegex { get; set; }

        [JsonProperty("NicknameRegex")]
        public string NicknameRegexString
        {
            get
            {
                return NicknameRegex?.ToString();
            }

            set
            {
                NicknameRegex = (value == null)
                    ? null
                    : new Regex(value, RegexOptions.Compiled);
            }
        }

        [JsonIgnore]
        [CanBeNull]
        public Regex MessageRegex { get; set; }

        [JsonProperty("MessageRegex")]
        public string MessageRegexString
        {
            get
            {
                return MessageRegex?.ToString();
            }

            set
            {
                MessageRegex = (value == null)
                    ? null
                    : new Regex(value, RegexOptions.Compiled);
            }
        }
    }
}
