using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Numerics;
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

        protected Dictionary<int, string> StateIDToName { get; set; }
        protected Dictionary<string, int> LowerNameToStateID { get; set; }
        protected Dictionary<int, BigInteger> StateIDToPop { get; set; }
        protected Dictionary<(int, DateTime), VaccinationStatsFields> StateIDAndDateToFields { get; set; }
        protected DateTimeOffset CoronaTimestamp { get; set; }
        protected int LongestKeyLength { get; set; }

        public StatsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new StatsConfig(config);

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("vaccine"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // location
                    ),
                    CommandUtil.MakeTags("fun"), // not really, but not a functional command either
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleVaccineCommand
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
            using (var request = new HttpRequestMessage(HttpMethod.Get, Config.VaccineCsvUri))
            using (var response = client.SendAsync(request, HttpCompletionOption.ResponseContentRead).Result)
            {
                responseText = response.Content.ReadAsStringAsync().Result;
            }

            // parse as CSV
            StateIDToName = new Dictionary<int, string>();
            LowerNameToStateID = new Dictionary<string, int>();
            StateIDToPop = new Dictionary<int, BigInteger>();
            StateIDAndDateToFields = new Dictionary<(int, DateTime), VaccinationStatsFields>();
            bool headerRow = true;
            foreach (string line in responseText.Split('\n'))
            {
                if (headerRow)
                {
                    headerRow = false;
                    continue;
                }

                string[] pieces = line.TrimEnd('\r', '\n').Split(';');

                string dateString = pieces[0].Split('T')[0];
                DateTime date = DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                int stateID = StringUtil.MaybeParseInt(pieces[1]).Value;

                string popString = pieces[2];
                if (!string.IsNullOrEmpty(popString))
                {
                    StateIDToPop[stateID] = ParseBigInt(popString);
                }

                string stateName = pieces[3];
                StateIDToName[stateID] = stateName;
                LowerNameToStateID[stateName.ToLowerInvariant()] = stateID;

                var fields = new VaccinationStatsFields
                {
                    Vaccinations = ParseBigInt(pieces[4]),
                    PartiallyImmune = ParseBigInt(pieces[6]),
                    FullyImmune = ParseBigInt(pieces[8]),
                };

                StateIDAndDateToFields[(stateID, date)] = fields;
            }
            CoronaTimestamp = DateTimeOffset.Now;
        }

        protected virtual void HandleVaccineCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string name = ((string)cmd.Arguments[0]).Trim();
            if (name.Length == 0)
            {
                name = Config.DefaultTarget;
            }
            string nameLower = name.ToLowerInvariant();

            TimeSpan updateDelta = DateTimeOffset.Now - CoronaTimestamp;
            if (updateDelta > TimeSpan.FromDays(1.0))
            {
                // update first
                UpdateStats();
            }

            // try to find the state
            int stateID;
            if (!LowerNameToStateID.TryGetValue(nameLower, out stateID))
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: Was ist das f\u00FCr 1 Bundesland?");
                return;
            }

            var freshestEntry = StateIDAndDateToFields
                .Where(sidf => sidf.Key.Item1 == stateID)
                .OrderByDescending(sidf => sidf.Key.Item2)
                .Select(sidf => sidf.Value)
                .First();
            BigInteger pop = StateIDToPop[stateID];

            decimal vacPercent = (decimal)(freshestEntry.Vaccinations * 10000 / pop) / 100.0m;
            decimal partPercent = (decimal)(freshestEntry.PartiallyImmune * 10000 / pop) / 100.0m;
            decimal fullPercent = (decimal)(freshestEntry.FullyImmune * 10000 / pop) / 100.0m;

            ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {StateIDToName[stateID]}: {freshestEntry.Vaccinations:#,###} ({vacPercent:0.00}%) vaccinations => {freshestEntry.PartiallyImmune:#,###} ({partPercent:0.00}%) at least partially, {freshestEntry.FullyImmune:#,###} ({fullPercent:0.00}%) fully immune");
        }

        static BigInteger ParseBigInt(string s)
            => BigInteger.Parse(s, NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
