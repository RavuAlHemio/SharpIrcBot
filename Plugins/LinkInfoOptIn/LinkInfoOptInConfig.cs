using LinkInfo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace LinkInfoOptIn
{
    [JsonObject(MemberSerialization.OptOut)]
    public class LinkInfoOptInConfig : LinkInfoConfig, IDatabaseModuleConfig
    {
        public string DatabaseProvider { get; set; }
        public string DatabaseConnectionString { get; set; }

        public LinkInfoOptInConfig(JObject obj)
            : base (obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
