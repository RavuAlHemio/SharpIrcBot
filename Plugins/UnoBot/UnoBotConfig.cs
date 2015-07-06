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

        public UnoBotConfig(JObject obj)
        {
            Curses = new List<string>();
            ManyCardsCurseThreshold = 4;
            ManyDrawsCurseThreshold = 3;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
