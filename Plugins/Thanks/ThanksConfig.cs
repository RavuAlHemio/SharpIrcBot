using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Thanks
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ThanksConfig : IDatabaseModuleConfig
    {
        public string DatabaseProviderAssembly { get; set; }
        public string DatabaseConfiguratorClass { get; set; }
        public string DatabaseConfiguratorMethod { get; set; }
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
