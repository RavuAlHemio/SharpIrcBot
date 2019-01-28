using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Config;

namespace SharpIrcBot.Plugins.Proverb
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ProverbConfig
    {
        public string ProverbURI { get; set; }
        public string NodeSelector { get; set; }
        public double TimeoutSeconds { get; set; }

        public ProverbConfig(JObject obj)
        {
            ProverbURI = "http://sprichwort.gener.at/or/";
            NodeSelector = ".//div[@class=\"spwort\"]";
            TimeoutSeconds = 5.0;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
