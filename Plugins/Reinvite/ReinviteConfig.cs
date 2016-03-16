using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reinvite
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ReinviteConfig
    {
        public bool RejoinOnInvite { get; set; }
        public bool RejoinOnPrivateMessage { get; set; }
        public bool AutoJoinedChannelsOnly { get; set; }

        public ReinviteConfig(JObject obj)
        {
            RejoinOnInvite = false;
            RejoinOnPrivateMessage = false;
            AutoJoinedChannelsOnly = true;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
