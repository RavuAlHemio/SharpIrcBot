using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Demoderation
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DemoderationConfig : IDatabaseModuleConfig
    {
        public string DatabaseProviderAssembly { get; set; }
        public string DatabaseConfiguratorClass { get; set; }
        public string DatabaseConfiguratorMethod { get; set; }
        public string DatabaseConnectionString { get; set; }

        public int BacklogSize { get; set; }
        public double BanMinutes { get; set; }
        public double AbuseBanMinutes { get; set; }
        public double AbuseLockMinutes { get; set; }
        public double CleanupPeriodMinutes { get; set; }

        public DemoderationConfig(JObject obj)
        {
            BacklogSize = 50;
            BanMinutes = 5.0;
            AbuseBanMinutes = 10.0;
            AbuseLockMinutes = 1440.0;
            CleanupPeriodMinutes = 1.0;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
