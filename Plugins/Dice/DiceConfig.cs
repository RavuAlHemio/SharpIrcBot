using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dice
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DiceConfig
    {
        public HashSet<string> Channels { get; set; }

        public List<string> ObstinateAnswers { get; set; }

        public List<string> YesNoAnswers { get; set; }

        public List<string> DecisionSplitters { get; set; }

        public int MaxRollCount { get; set; }

        public int MaxDiceCount { get; set; }

        public int MaxSideCount { get; set; }

        public DiceConfig(JObject obj)
        {
            Channels = new HashSet<string>();
            ObstinateAnswers = new List<string>();
            YesNoAnswers = new List<string>();
            DecisionSplitters = new List<string>();

            MaxRollCount = 16;
            MaxDiceCount = 1024;
            MaxSideCount = 1048576;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
