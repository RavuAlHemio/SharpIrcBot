using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Trivia
{
    [JsonObject(MemberSerialization.OptOut)]
    public class TriviaConfig : IDatabaseModuleConfig
    {
        public string DatabaseProviderAssembly { get; set; }
        public string DatabaseConfiguratorClass { get; set; }
        public string DatabaseConfiguratorMethod { get; set; }
        public string DatabaseConnectionString { get; set; }

        public string TriviaChannel { get; set; }
        public double SecondsBetweenHints { get; set; }
        public int HintCount { get; set; }
        public List<string> QuestionFiles { get; set; }

        public TriviaConfig(JObject obj)
        {
            SecondsBetweenHints = 15.0;
            HintCount = 3;
            QuestionFiles = new List<string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
