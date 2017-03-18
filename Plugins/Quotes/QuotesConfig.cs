using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Quotes
{
    [JsonObject(MemberSerialization.OptOut)]
    public class QuotesConfig : IDatabaseModuleConfig
    {
        public string DatabaseProviderAssembly { get; set; }
        public string DatabaseConfiguratorClass { get; set; }
        public string DatabaseConfiguratorMethod { get; set; }
        public string DatabaseConnectionString { get; set; }

        public int RememberForQuotes { get; set; }
        public int VoteThreshold { get; set; }

        public QuotesConfig(JObject obj)
        {
            RememberForQuotes = 30;
            VoteThreshold = -3;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
