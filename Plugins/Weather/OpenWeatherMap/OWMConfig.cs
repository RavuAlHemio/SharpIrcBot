using System;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Weather.OpenWeatherMap
{
    [JsonObject(MemberSerialization.OptOut)]
    public class OWMConfig
    {
        public string ApiKey { get; set; }

        public int? MaxCallsPerMinute { get; set; }

        [JsonIgnore]
        public TimeSpan Timeout { get; set; }

        public double TimeoutSeconds
        {
            get { return Timeout.TotalSeconds; }
            set { Timeout = TimeSpan.FromSeconds(value); }
        }

        public OWMConfig()
        {
            Timeout = TimeSpan.FromSeconds(5.0);
        }
    }
}
