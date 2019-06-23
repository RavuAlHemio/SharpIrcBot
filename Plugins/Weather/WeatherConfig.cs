using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Config;
using SharpIrcBot.Plugins.Libraries.GeoNames;

namespace SharpIrcBot.Plugins.Weather
{
    [JsonObject(MemberSerialization.OptOut)]
    public class WeatherConfig
    {
        public GeoNamesConfig GeoNames { get; set; }

        public string DefaultLocation { get; set; }

        public Dictionary<string, string> LocationAliases { get; set; }

        public List<PluginConfig> WeatherProviders { get; set; }

        public WeatherConfig(JObject obj)
        {
            GeoNames = new GeoNamesConfig();
            LocationAliases = new Dictionary<string, string>();
            WeatherProviders = new List<PluginConfig>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
