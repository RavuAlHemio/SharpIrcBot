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

        public string Read()
        {
            string json = _client
                .GetStringAsync(_config.Uri)
                .SyncWait();

            var entries = new List<NightscoutSGVEntry>();
            JsonConvert.PopulateObject(json, entries);

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

            return $"{newest.SGV} mg/dL ({mmolL:0.00} mmol/L) at {newest.Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }
}
