using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Messenger
{
    [JsonObject(MemberSerialization.OptOut)]
    public class MessengerConfig : IDatabaseModuleConfig
    {
        public string DatabaseProvider { get; set; }
        public string DatabaseConnectionString { get; set; }
        public int TooManyMessages { get; set; }
        public int MaxMessagesToReplay { get; set; }
        public ISet<string> DeliveryChannels { get; set; }

        public MessengerConfig(JObject obj)
        {
            TooManyMessages = 10;
            MaxMessagesToReplay = 10;
            DeliveryChannels = new HashSet<string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
