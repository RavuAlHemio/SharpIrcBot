using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Calc
{
    [JsonObject(MemberSerialization.OptOut)]
    public class CalcConfig
    {
        public double TimeoutSeconds { get; set; }
        public int MaxResultStringLength { get; set; }

        public CalcConfig(JObject obj)
        {
            TimeoutSeconds = 3.0;
            MaxResultStringLength = 2048;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
