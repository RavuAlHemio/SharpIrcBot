using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PlayerConfig
    {
        public string CasinoBotName { get; set; }
        public string CasinoChannel { get; set; }
        public List<string> Curses { get; set; }
        public List<string> Gloats { get; set; }
        public int LowStandNum { get; set; }
        public int LowStandDen { get; set; }
        public int HighStandNum { get; set; }
        public int HighStandDen { get; set; }

        public PlayerConfig(JObject obj)
        {
            CasinoBotName = "CasinoBot";
            CasinoChannel = "#casino";
            Curses = new List<string>();
            Gloats = new List<string>();
            LowStandNum = 1;
            LowStandDen = 5;
            HighStandNum = 4;
            HighStandDen = 5;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
