using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace IKnewThat
{
    [JsonObject(MemberSerialization.OptOut)]
    public class IKnewThatConfig : IDatabaseModuleConfig
    {
        public string DatabaseProviderAssembly { get; set; }
        public string DatabaseConfiguratorClass { get; set; }
        public string DatabaseConfiguratorMethod { get; set; }
        public string DatabaseConnectionString { get; set; }

        public IKnewThatConfig(JObject obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
