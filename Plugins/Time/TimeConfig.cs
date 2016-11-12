using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Time
{
    [JsonObject(MemberSerialization.OptOut)]
    public class TimeConfig
    {
        public string GeoNamesUsername { get; set; }

        public string DefaultLocation { get; set; }

        public double TimeoutSeconds { get; set; }

        public TimeConfig(JObject obj)
        {
            TimeoutSeconds = 5.0;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
