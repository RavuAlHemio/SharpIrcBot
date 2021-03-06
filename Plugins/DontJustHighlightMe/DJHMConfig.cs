﻿using System;
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

        public HashSet<string> ImmuneNicksOrUsernames { get; set; }

        [JsonProperty("NonNicknameRegex")]
        public string NonNicknameRegexString
        {
            get { return NickDelimiterRegex.ToString(); }
            set { NickDelimiterRegex = new Regex(value, RegexOptions.Compiled); }
        }

        [JsonIgnore]
        public Regex NickDelimiterRegex { get; set; }

        [JsonProperty("NotJustAHighlightRegex")]
        public string NotJustAHighlightRegexString
        {
            get { return NotJustAHighlightRegex.ToString(); }
            set { NotJustAHighlightRegex = new Regex(value, RegexOptions.Compiled); }
        }

        [JsonIgnore]
        public Regex NotJustAHighlightRegex { get; set; }

        [JsonProperty("NickToIgnoreRegex")]
        public string NickToIgnoreRegexString
        {
            get { return NickToIgnoreRegex?.ToString(); }
            set { NickToIgnoreRegex = (value == null) ? null : new Regex(value, RegexOptions.Compiled); }
        }

        [JsonIgnore]
        public Regex NickToIgnoreRegex { get; set; }

        public bool Kick { get; set; }

        public string KickMessage { get; set; }

        public int? TriggerPercentage { get; set; }

        public int DelayMinMessages { get; set; }

        public int DelayMaxMessages { get; set; }

        public DJHMConfig(JObject obj)
        {
            Channels = new HashSet<string>();
            UserAliases = new Dictionary<string, string>();
            ImmuneNicksOrUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            NickDelimiterRegex = new Regex("[^\\p{L}\\p{N}_\\\\\\[\\]{}^`|-]+", RegexOptions.Compiled);
            NotJustAHighlightRegex = new Regex("[:;]-?[()/\\\\$!|]|\\^\\^|:[a-z0-9^]+:", RegexOptions.Compiled);
            NickToIgnoreRegex = null;
            Kick = false;
            KickMessage = "Don't just highlight someone, tell them what you want!";
            TriggerPercentage = null;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
