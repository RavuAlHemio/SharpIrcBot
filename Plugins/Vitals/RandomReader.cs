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
        public class RandomReaderValue
        {
            public decimal MinValue { get; set; }
            public decimal MaxValue { get; set; }
            public decimal Resolution { get; set; }
            public int ConversionReference { get; set; }
            public decimal ConversionFactor { get; set; }

            public RandomReaderValue()
            {
                ConversionReference = -1;
            }
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class RandomReaderConfig
        {
            public List<RandomReaderValue> Values { get; set; }
            public string FormatString { get; set; }

            public RandomReaderConfig(JObject obj)
            {
                Values = new List<RandomReaderValue>();

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
            var finalValues = new decimal[_config.Values.Count];
            for (int i = 0; i < finalValues.Length; ++i)
            {
                RandomReaderValue val = _config.Values[i];
                if (val.ConversionReference >= 0)
                {
                    finalValues[i] = finalValues[val.ConversionReference] * val.ConversionFactor;
                }
                else
                {
                    var span = (int)((val.MaxValue - val.MinValue) / val.Resolution);
                    int add = _rng.Next(span);
                    finalValues[i] = val.MinValue + (add * val.Resolution);
                }
            }

            return string.Format(_config.FormatString, finalValues.Cast<object>().ToArray());
        }
    }
}
