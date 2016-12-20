using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Weather
{
    [JsonObject(MemberSerialization.OptOut)]
    public class WeatherConfig
    {
        public string WunderApiKey { get; set; }

        public string DefaultLocation { get; set; }

        public int MaxRequestsPerMinute { get; set; }

        public int MaxRequestsPerESTDay { get; set; }

        public List<string> CoolDownResponses { get; set; }

        public double TimeoutSeconds { get; set; }

        public Dictionary<string, string> LocationAliases { get; set; }

        public WeatherConfig(JObject obj)
        {
            MaxRequestsPerMinute = 10;
            MaxRequestsPerESTDay = 500;
            CoolDownResponses = new List<string>();
            TimeoutSeconds = 5.0;
            LocationAliases = new Dictionary<string, string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
