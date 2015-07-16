using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace DatabaseNickMapping
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DatabaseNickMappingConfig : IDatabaseModuleConfig
    {
        public string DatabaseProvider { get; set; }
        public string DatabaseConnectionString { get; set; }

        public DatabaseNickMappingConfig(JObject obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
