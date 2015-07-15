using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnoBot
{
    [JsonObject(MemberSerialization.OptOut)]
    public class UnoBotConfig
    {
        public string UnoChannel { get; set; }
        public List<string> Curses { get; set; }
        public int ManyCardsCurseThreshold { get; set; }
        public int ManyDrawsCurseThreshold { get; set; }
        public int PlayToWinThreshold { get; set; }
        public bool DrawAllTheTime { get; set; }
        public int StandardColorMatchPriority { get; set; }
        public int StandardValueMatchPriority { get; set; }
        public int StandardColorAndValueMatchPriority { get; set; }
        public int StandardReorderPriority { get; set; }
        public int StandardColorChangePriority { get; set; }
        public int StrategicDrawDenominator { get; set; }

        public UnoBotConfig(JObject obj)
        {
            Curses = new List<string>();
            ManyCardsCurseThreshold = 4;
            ManyDrawsCurseThreshold = 3;
            PlayToWinThreshold = 2;
            DrawAllTheTime = false;
            StandardColorMatchPriority = 2;
            StandardValueMatchPriority = 3;
            StandardColorAndValueMatchPriority = 1;
            StandardReorderPriority = 1;
            StandardColorChangePriority = 1;
            StrategicDrawDenominator = 20;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
