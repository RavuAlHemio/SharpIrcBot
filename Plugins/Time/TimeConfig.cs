using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Time
{
    [JsonObject(MemberSerialization.OptOut)]
    public class TimeConfig
    {
        public string GeoNamesUsername { get; set; }

        public string DefaultLocation { get; set; }

        public string TimeZoneDatabaseFile { get; set; }

        public double TimeoutSeconds { get; set; }

        public Dictionary<string, string> LocationAliases { get; set; }

        public TimeConfig(JObject obj)
        {
            TimeZoneDatabaseFile = "tzdb.nzd";
            TimeoutSeconds = 5.0;
            LocationAliases = new Dictionary<string, string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
