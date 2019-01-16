using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.Vitals;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Vitals
{
    public class RandomReader : IVitalsReader
    {
        [JsonObject(MemberSerialization.OptOut)]
        public class RandomReaderConfig
        {
            public decimal MinValue { get; set; }
            public decimal MaxValue { get; set; }
            public decimal Resolution { get; set; }
            public string FormatString { get; set; }

            public RandomReaderConfig(JObject obj)
            {
                var ser = new JsonSerializer();
                ser.Populate(obj.CreateReader(), this);
            }
        }

        private RandomReaderConfig _config;
        private Random _rng;

        public RandomReader(JObject options)
        {
            _config = new RandomReaderConfig(options);
            _rng = new Random();
        }

        public string Read()
        {
            var span = (int)((_config.MaxValue - _config.MinValue) / _config.Resolution);
            int add = _rng.Next(span);
            decimal finalValue = _config.MinValue + (add * _config.Resolution);

            return string.Format(_config.FormatString, finalValue);
        }
    }
}
