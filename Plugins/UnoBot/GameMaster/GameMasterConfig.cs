using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnoBot.GameMaster
{
    [JsonObject(MemberSerialization.OptOut)]
    public class GameMasterConfig
    {
        public string UnoChannel { get; set; }

        public GameMasterConfig(JObject obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}

