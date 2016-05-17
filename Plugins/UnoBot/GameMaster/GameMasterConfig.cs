using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnoBot.GameMaster
{
    [JsonObject(MemberSerialization.OptOut)]
    public class GameMasterConfig
    {
        public string UnoChannel { get; set; }
        public double SecondsPerTurn { get; set; }
        public double BotJoinWaitSeconds { get; set; }
        public int InitialDealSize { get; set; }
        public bool ShufflePlayerList { get; set; }

        public GameMasterConfig(JObject obj)
        {
            SecondsPerTurn = 60.0;
            BotJoinWaitSeconds = 3.0;
            InitialDealSize = 7;
            ShufflePlayerList = false;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
