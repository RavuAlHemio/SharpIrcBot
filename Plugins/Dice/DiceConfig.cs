using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Dice
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DiceConfig
    {
        public HashSet<string> Channels { get; set; }

        public List<string> ObstinateAnswers { get; set; }

        public List<string> YesNoAnswers { get; set; }

        public List<string> DecisionSplitters { get; set; }

        public List<string> SpecialDecisionAnswers { get; set; }

        public List<string> CooldownAnswers { get; set; }

        public int SpecialDecisionAnswerPercent { get; set; }

        public int MaxRollCount { get; set; }

        public int MaxDiceCount { get; set; }

        public int MaxSideCount { get; set; }

        public long CooldownPerCommandUsage { get; set; }

        public long CooldownUpperBoundary { get; set; }

        public string DefaultWikipediaLanguage { get; set; }

        public DiceConfig(JObject obj)
        {
            Channels = new HashSet<string>();
            ObstinateAnswers = new List<string>();
            YesNoAnswers = new List<string>();
            DecisionSplitters = new List<string>();
            SpecialDecisionAnswers = new List<string>();
            CooldownAnswers = new List<string>();

            SpecialDecisionAnswerPercent = 10;
            MaxRollCount = 16;
            MaxDiceCount = 1024;
            MaxSideCount = 1048576;

            CooldownPerCommandUsage = 4;
            CooldownUpperBoundary = 32;

            DefaultWikipediaLanguage = "en";

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
