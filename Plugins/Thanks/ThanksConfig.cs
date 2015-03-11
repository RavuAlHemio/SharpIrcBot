using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Thanks
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ThanksConfig : IDatabaseModuleConfig
    {
        public string DatabaseProvider { get; set; }
        public string DatabaseConnectionString { get; set; }
        public int MostGratefulCount { get; set; }
        public string MostGratefulCountText { get; set; }
        public int MostThankedCount { get; set; }

        public ThanksConfig(JObject obj)
        {
            MostGratefulCount = 5;
            MostGratefulCountText = "five";
            MostThankedCount = 5;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
