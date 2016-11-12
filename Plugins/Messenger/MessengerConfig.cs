using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Messenger
{
    [JsonObject(MemberSerialization.OptOut)]
    public class MessengerConfig : IDatabaseModuleConfig
    {
        public string DatabaseProviderAssembly { get; set; }
        public string DatabaseConfiguratorClass { get; set; }
        public string DatabaseConfiguratorMethod { get; set; }
        public string DatabaseConnectionString { get; set; }

        public int TooManyMessages { get; set; }
        public int MaxMessagesToReplay { get; set; }
        public bool AllowMulticast { get; set; }
        public ISet<string> DeliveryChannels { get; set; }

        public MessengerConfig(JObject obj)
        {
            TooManyMessages = 10;
            MaxMessagesToReplay = 10;
            AllowMulticast = false;
            DeliveryChannels = new HashSet<string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
