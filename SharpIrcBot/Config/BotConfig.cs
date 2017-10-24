using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Config
{
    [JsonObject(MemberSerialization.OptOut)]
    public class BotConfig
    {
        [NotNull]
        public string ServerHostname { get; set; }
        public int ServerPort { get; set; }
        public bool UseTls { get; set; }
        public bool VerifyTlsCertificate { get; set; }

        [NotNull]
        public string Nickname { get; set; }
        [NotNull]
        public string Username { get; set; }
        [NotNull]
        public string DisplayName { get; set; }
        [CanBeNull]
        public string ServerPassword { get; set; }
        [NotNull]
        public string Encoding { get; set; }

        public int SendDelay { get; set; }
        public double CooldownIncreaseThresholdMinutes { get; set; }
        [NotNull, ItemNotNull]
        public List<string> AutoConnectCommands { get; set; }
        [NotNull, ItemNotNull]
        public List<string> AutoJoinChannels { get; set; }
        [NotNull, ItemNotNull]
        public List<PluginConfig> Plugins { get; set; }

        [NotNull]
        public string CtcpVersionResponse { get; set; }
        [NotNull]
        public string CtcpFingerResponse { get; set; }

        [NotNull, ItemNotNull]
        public ISet<string> BannedUsers { get; set; }

        public CommandConfig Commands { get; set; }

        [JsonProperty("NicknameRegex")]
        public string NicknameRegexString
        {
            get { return NicknameRegex.ToString(); }
            set { NicknameRegex = new Regex(value, RegexOptions.Compiled); }
        }

        [JsonIgnore]
        public Regex NicknameRegex { get; set; }

        public BotConfig(JObject obj)
        {
            ServerPort = 6669;
            UseTls = false;
            VerifyTlsCertificate = false;

            Encoding = "utf-8";

            SendDelay = 200;
            CooldownIncreaseThresholdMinutes = 1.0;
            AutoConnectCommands = new List<string>();
            AutoJoinChannels = new List<string>();
            Plugins = new List<PluginConfig>();

            CtcpVersionResponse = "SharpIrcBot";
            CtcpFingerResponse = "I am a bot. I have no fingers.";

            //BannedUsers = new HashSet<string>();
            BannedUsers = new FilteringSetWrapper<string>(
                new HashSet<string>(),
                s => s.ToLowerInvariant()
            );

            Commands = new CommandConfig();

            NicknameRegex = new Regex("^[a-zA-Z_\\\\\\[\\]{}^`|][a-zA-Z0-9_\\\\\\[\\]{}^`|-]*$", RegexOptions.Compiled);

            JsonSerializer.CreateDefault().Populate(obj.CreateReader(), this);

            if (Username == null)
            {
                Username = Nickname;
            }
            else if (Nickname == null)
            {
                Nickname = Username;
            }
        }
    }
}
