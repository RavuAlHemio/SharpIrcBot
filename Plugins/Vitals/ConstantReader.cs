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
    public class ConstantReader : IVitalsReader
    {
        [JsonObject(MemberSerialization.OptOut)]
        public class ConstantReaderConfig
        {
            public string ConstantText { get; set; }

            public ConstantReaderConfig(JObject obj)
            {
                var ser = new JsonSerializer();
                ser.Populate(obj.CreateReader(), this);
            }
        }

        private ConstantReaderConfig _config;

        public ConstantReader(JObject options)
        {
            _config = new ConstantReaderConfig(options);
        }

        public string Read()
        {
            return _config.ConstantText;
        }
    }
}
