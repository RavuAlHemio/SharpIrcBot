using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Config
{
    [JsonObject(MemberSerialization.OptOut)]
    public class CommandConfig
    {
        public string CommandPrefix { get; set; }
        public bool AllowWhitespaceBeforeCommandPrefix { get; set; }

        public CommandConfig()
        {
            CommandPrefix = "!";
            AllowWhitespaceBeforeCommandPrefix = false;
        }
    }
}
