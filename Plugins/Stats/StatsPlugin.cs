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

        protected Dictionary<string, long> CoronaCasesPerDistrict { get; set; }
        protected DateTimeOffset CoronaTimestamp { get; set; }
        protected Dictionary<string, long> PopulationPerDistrict { get; set; }
        protected int LongestDistrictNameLength { get; set; }

        public StatsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new StatsConfig(config);

            CoronaCasesPerDistrict = new Dictionary<string, long>();
            PopulationPerDistrict = new Dictionary<string, long>();

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
            var casesDict = new Dictionary<string, long>();
            var entries = JArray.Parse(responseText);
            foreach (var entry in entries.OfType<JObject>())
            {
                var districtName = (string)entry["label"];
                var cases = (long)entry["y"];
                casesDict[districtName] = cases;
            }
            CoronaCasesPerDistrict = casesDict;

            // load population stats
            var popDict = new Dictionary<string, long>();
            string popFileName = Path.Combine(SharpIrcBotUtil.AppDirectory, Config.DistrictPopFile);
            using (var stream = File.Open(popFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, StringUtil.Utf8NoBom))
            {
                JsonSerializer.Create().Populate(reader, popDict);
            }
            PopulationPerDistrict = popDict;

            LongestDistrictNameLength = PopulationPerDistrict
                .Keys
                .Max(districtName => (int?)districtName.Length)
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
            if (nameLower.Length > 2*LongestDistrictNameLength)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: Try a shorter district name...");
                return;
            }

            TimeSpan updateDelta = DateTimeOffset.Now - CoronaTimestamp;
            if (updateDelta > TimeSpan.FromDays(1.0))
            {
                // update first
                UpdateStats();
            }

            string finalName;
            long finalCases;
            long finalPop;

            // special-case Vienna (sum up districts)
            if (nameLower == "wien")
            {
                finalName = "Wien (total)";
                finalCases = 0;
                finalPop = 0;

                foreach (var kvp in PopulationPerDistrict)
                {
                    long hereCases;
                    if (!CoronaCasesPerDistrict.TryGetValue(kvp.Key, out hereCases))
                    {
                        hereCases = 0;
                    }

                    finalCases += hereCases;
                    finalPop += kvp.Value;
                }
            }
            else
            {
                // find the closest district by Levenshtein distance
                long bestDistance = long.MaxValue;
                string bestName = null;
                string bestKey = null;
                long bestPop = long.MinValue;
                foreach (var kvp in PopulationPerDistrict)
                {
                    string districtName = kvp.Key;
                    string districtKey = districtName.ToLowerInvariant();

                    // remove trailing "(stadt)"
                    if (districtKey.EndsWith("(stadt)"))
                    {
                        districtKey = districtKey
                            .Substring(0, districtKey.Length - "(stadt)".Length)
                            .TrimEnd(' ');
                    }

                    // Vienna: clean up weird district format "wien  0.,hauptdorf"
                    if (districtKey.StartsWith("wien ") && districtKey.Contains(".,"))
                    {
                        int dotCommaIndex = districtKey.IndexOf(".,");
                        districtKey = "wien-" + districtKey.Substring(dotCommaIndex + ".,".Length);
                    }

                    if (bestName == null)
                    {
                        bestName = districtName;
                        bestKey = districtKey;
                        bestPop = kvp.Value;
                        continue;
                    }

                    long thisDistance = StringUtil.LevenshteinDistance(nameLower, districtKey);
                    if (bestDistance > thisDistance)
                    {
                        bestDistance = thisDistance;
                        bestName = districtName;
                        bestKey = districtKey;
                        bestPop = kvp.Value;
                    }
                }

                if (bestName == null)
                {
                    ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: Failed to find this district.");
                    return;
                }

                // get cases
                long cases;
                if (!CoronaCasesPerDistrict.TryGetValue(bestName, out cases))
                {
                    cases = 0;
                }

                finalName = bestName;
                finalCases = cases;
                finalPop = bestPop;
            }

            // calculate cases vs. population
            double perTenThousand = (finalCases * 10_000.0) / finalPop;

            ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {finalName}: {finalCases} cases ({perTenThousand:0.00} per 10k inhabitants)");
        }
    }
}
