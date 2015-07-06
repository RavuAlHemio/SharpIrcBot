using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Quotes
{
    [JsonObject(MemberSerialization.OptOut)]
    public class QuotesConfig : IDatabaseModuleConfig
    {
        public string DatabaseProvider { get; set; }
        public string DatabaseConnectionString { get; set; }
        public int RememberForQuotes { get; set; }

        public QuotesConfig(JObject obj)
        {
            RememberForQuotes = 30;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
