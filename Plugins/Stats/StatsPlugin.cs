using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
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

        public static readonly Regex EcmaScriptVarDef = new Regex("^\\s*var\\s+[a-zA-Z_$][a-zA-Z0-9_$]*\\s*=\\s*", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager { get; }
        protected StatsConfig Config { get; set; }

        protected DrillDownValue<string, long> CoronaStatsRoot { get; set; }
        protected DateTimeOffset CoronaTimestamp { get; set; }
        protected int LongestKeyLength { get; set; }

        const string PopulationMetric = "population";
        const string Covid19CasesMetric = "covid-19-cases";

        public StatsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new StatsConfig(config);

            CoronaStatsRoot = null;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("autcorona"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // location
                    ),
                    CommandUtil.MakeTags("fun"), // not really, but not a functional command either
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleAutCoronaCommand
            );

            UpdateStats();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new StatsConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            UpdateStats();
        }

        protected virtual void UpdateStats()
        {
            // obtain the data
            var client = new HttpClient();
            if (Config.TimeoutSeconds.HasValue)
            {
                client.Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds.Value);
            }

            string responseText;
            using (var request = new HttpRequestMessage(HttpMethod.Get, Config.DistrictCoronaStatsUri))
            using (var response = client.SendAsync(request, HttpCompletionOption.ResponseContentRead).Result)
            {
                responseText = response.Content.ReadAsStringAsync().Result;
            }

            // strip variable definition, leaving data
            Match m = EcmaScriptVarDef.Match(responseText);
            if (m.Success)
            {
                // turn variable definition into data
                responseText = responseText.Substring(m.Length)
                    .TrimEnd(';', '\r', '\n');
            }

            // load as JSON
            var districtToCaseCount = new Dictionary<string, long>();
            var entries = JArray.Parse(responseText);
            foreach (var entry in entries.OfType<JObject>())
            {
                var districtName = (string)entry["label"];
                var cases = (long)entry["y"];
                districtToCaseCount[districtName] = cases;
            }

            // load population stats
            var stateToDistrictToPopulation = new Dictionary<string, Dictionary<string, long>>();
            string popFileName = Path.Combine(SharpIrcBotUtil.AppDirectory, Config.StateDistrictPopFile);
            using (var stream = File.Open(popFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, StringUtil.Utf8NoBom))
            {
                JsonSerializer.Create().Populate(reader, stateToDistrictToPopulation);
            }

            // generate the tree out of the population dict
            var austria = new DrillDownValue<string, long>();
            austria.Keys.Add("\u00D6sterreich");
            foreach (var kvp1 in stateToDistrictToPopulation)
            {
                string stateName = kvp1.Key;
                var state = new DrillDownValue<string, long>();
                state.Keys.Add(stateName);
                austria.Children.Add(state);

                foreach (var kvp2 in kvp1.Value)
                {
                    string districtName = kvp2.Key;
                    long population = kvp2.Value;

                    var district = new DrillDownValue<string, long>();
                    district.Keys.Add(districtName);
                    state.Children.Add(district);

                    // alternative key: no trailing "(stadt)"
                    if (districtName.ToLowerInvariant().EndsWith("(stadt)"))
                    {
                        string shortKey = districtName
                            .Substring(0, districtName.Length - "(stadt)".Length)
                            .TrimEnd(' ');
                        district.Keys.Add(shortKey);
                    }

                    district.Values[PopulationMetric] = population;

                    long covid19Cases;
                    if (!districtToCaseCount.TryGetValue(districtName, out covid19Cases))
                    {
                        covid19Cases = 0;
                    }
                    district.Values[Covid19CasesMetric] = covid19Cases;
                }
            }
            CoronaStatsRoot = austria;

            LongestKeyLength = CoronaStatsRoot
                .FlattenedDescendants()
                .SelectMany(ddv => ddv.Keys)
                .Max(key => (int?)key.Length)
                ?? 0;

            CoronaTimestamp = DateTimeOffset.Now;
        }

        protected virtual void HandleAutCoronaCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string name = ((string)cmd.Arguments[0]).Trim();
            if (name.Length == 0)
            {
                name = Config.DefaultTarget;
            }
            string nameLower = name.ToLowerInvariant();

            // ensure our users don't go overboard
            if (nameLower.Length > 2*LongestKeyLength)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: Try a shorter key...");
                return;
            }

            TimeSpan updateDelta = DateTimeOffset.Now - CoronaTimestamp;
            if (updateDelta > TimeSpan.FromDays(1.0))
            {
                // update first
                UpdateStats();
            }

            DrillDownValue<string, long> bestValue = null;
            long bestDistance = long.MaxValue;

            foreach (DrillDownValue<string, long> ddv in CoronaStatsRoot.FlattenedDescendants())
            {
                foreach (string key in ddv.Keys)
                {
                    // normalize the key
                    string normalizedKey = key.ToLowerInvariant();

                    // compare
                    long thisDistance = StringUtil.LevenshteinDistance(nameLower, normalizedKey);
                    if (bestDistance > thisDistance)
                    {
                        bestValue = ddv;
                        bestDistance = thisDistance;
                    }
                }
            }

            if (bestValue == null)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: Failed to find this area.");
                return;
            }

            string foundName = bestValue.Keys.First();

            // get population
            long pop = bestValue.TotalValueForMetric(
                PopulationMetric,
                initialValue: 0,
                missingValue: 0,
                meldValueFunc: (a, b) => a + b
            );

            // get cases
            long cases = bestValue.TotalValueForMetric(
                Covid19CasesMetric,
                initialValue: 0,
                missingValue: 0,
                meldValueFunc: (a, b) => a + b
            );

            // calculate cases vs. population
            double perTenThousand = (cases * 10_000.0) / pop;

            ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {foundName}: {cases} cases ({perTenThousand:0.00} per 10k inhabitants)");
        }
    }
}
