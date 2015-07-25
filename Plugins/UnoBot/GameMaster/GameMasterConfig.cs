using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnoBot.GameMaster
{
    [JsonObject(MemberSerialization.OptOut)]
    public class GameMasterConfig
    {
        public string UnoChannel { get; set; }
        public double SecondsPerTurn { get; set; }
        public int InitialDealSize { get; set; }

        public GameMasterConfig(JObject obj)
        {
            SecondsPerTurn = 60.0;
            InitialDealSize = 7;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
