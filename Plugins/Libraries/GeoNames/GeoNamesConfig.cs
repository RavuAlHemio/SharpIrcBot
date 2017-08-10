using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Libraries.GeoNames
{
    [JsonObject(MemberSerialization.OptOut)]
    public class GeoNamesConfig
    {
        public string Username { get; set; }
    }
}
