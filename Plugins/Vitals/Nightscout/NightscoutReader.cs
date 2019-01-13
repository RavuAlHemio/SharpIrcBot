using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.Vitals;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Vitals.Nightscout
{
    public class NightscoutReader : IVitalsReader
    {
        private HttpClient _client;
        private NightscoutConfig _config;

        public NightscoutReader(JObject options)
        {
            _config = new NightscoutConfig(options);
            _client = new HttpClient();
        }

        protected virtual string GetSGV()
        {
            if (_config.SGVUri == null)
            {
                return null;
            }

            string sgvJSON = _client
                .GetStringAsync(_config.SGVUri)
                .SyncWait();

            var entries = new List<NightscoutSGVEntry>();
            JsonConvert.PopulateObject(sgvJSON, entries);

            // find the newest value that is not in the future
            NightscoutSGVEntry newest = entries
                .Where(e => e.Timestamp <= DateTimeOffset.Now)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();

            if (newest == null)
            {
                return "No entry found!";
            }

            // FIXME: always mg/dl?
            decimal mmolL = newest.SGV * VitalsConstants.MmolLInMgDlGlucose;

            return $"sensor: {newest.SGV} mg/dL ({mmolL:0.00} mmol/L) at {newest.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
        }

        protected virtual string GetMBG()
        {
            if (_config.MBGUri == null)
            {
                return null;
            }

            string mbgJSON = _client
                .GetStringAsync(_config.MBGUri)
                .SyncWait();

            var entries = new List<NightscoutMBGEntry>();
            JsonConvert.PopulateObject(mbgJSON, entries);

            // find the newest value that is not in the future
            NightscoutMBGEntry newest = entries
                .Where(e => e.Timestamp <= DateTimeOffset.Now)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();

            if (newest == null)
            {
                return "No entry found!";
            }

            // FIXME: always mg/dl?
            decimal mmolL = newest.MBG * VitalsConstants.MmolLInMgDlGlucose;

            return $"last poke: {newest.MBG} mg/dL ({mmolL:0.00} mmol/L) at {newest.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
        }

        public string Read()
        {
            string sgv = GetSGV();
            string mbg = GetMBG();
            var pieces = new List<string> { sgv, mbg };

            string fullString = string.Join(
                "; ",
                pieces.Where(p => p != null)
            );
            return fullString;
        }
    }
}
