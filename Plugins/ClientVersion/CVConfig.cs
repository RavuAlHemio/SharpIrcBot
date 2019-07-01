using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.ClientVersion
{
    [JsonObject(MemberSerialization.OptOut)]
    public class CVConfig : IDatabaseModuleConfig
    {
        public string DatabaseProviderAssembly { get; set; }
        public string DatabaseConfiguratorClass { get; set; }
        public string DatabaseConfiguratorMethod { get; set; }
        public string DatabaseConnectionString { get; set; }

        public double? RescanIntervalDays { get; set; }

        public CVConfig(JObject obj)
        {
            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
