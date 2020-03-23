using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Stats
{
    public class StatsPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<StatsPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected StatsConfig Config { get; set; }

        public StatsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new StatsConfig(config);

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("corona"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // location
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleCoronaCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new StatsConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleCoronaCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string name = ((string)cmd.Arguments[0]).Trim();
            if (name.Length == 0)
            {
                name = Config.DefaultTarget;
            }
            string nameLower = name.ToLowerInvariant();

            var client = new HttpClient();
            if (Config.TimeoutSeconds.HasValue)
            {
                client.Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds.Value);
            }

            JObject doc;
            using (var request = new HttpRequestMessage(HttpMethod.Get, Config.CoronaUri))
            {
                using (var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result)
                using (Stream responseStream = response.Content.ReadAsStreamAsync().Result)
                using (var responseReader = new StreamReader(responseStream, Encoding.UTF8))
                using (var responseJsonReader = new JsonTextReader(responseReader))
                {
                    doc = JObject.Load(responseJsonReader);
                }
            }

            IEnumerable<JObject> entries = doc["rawData"]
                .OfType<JObject>()
                .Where(tok => tok["Country/Region"].Value<string>().ToLowerInvariant() == nameLower);
            long confirmed = 0, deaths = 0, recovered = 0;
            bool foundAny = false;
            foreach (JObject entry in entries)
            {
                foundAny = true;

                if (entry["Confirmed"] != null)
                {
                    confirmed += entry["Confirmed"].Value<long>();
                }
                if (entry["Deaths"] != null)
                {
                    deaths += entry["Deaths"].Value<long>();
                }
                if (entry["Recovered"] != null)
                {
                    recovered += entry["Recovered"].Value<long>();
                }
            }

            if (foundAny)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {name}: {confirmed} confirmed, {deaths} deaths, {recovered} recovered");
            }
            else
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {name} not found");
            }
        }
    }
}
