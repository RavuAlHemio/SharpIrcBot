using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Config;

namespace SharpIrcBot.Plugins.LinkInfo
{
    [JsonObject(MemberSerialization.OptOut)]
    public class LinkInfoConfig
    {
        public bool AutoShowLinkInfo { get; set; }
        public long MaxDownloadSizeBytes { get; set; }
        public double TimeoutSeconds { get; set; }
        public string FakeUserAgent { get; set; }
        public string GoogleDomain { get; set; }
        public Dictionary<string, string> FakeResponses { get; set; }
        public string TLDListFile { get; set; }
        public int MaxRedirects { get; set; }
        public Dictionary<string, string> DomainAnnotations { get; set; }
        public List<PluginConfig> LinkResolverPlugins { get; set; }

        public LinkInfoConfig(JObject obj)
        {
            MaxDownloadSizeBytes = 10 * 1024 * 1024;
            TimeoutSeconds = 5.0;
            FakeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:40.0) Gecko/20100101 Firefox/40.0";
            GoogleDomain = "www.google.at";
            FakeResponses = new Dictionary<string, string>();
            TLDListFile = null;
            MaxRedirects = 16;
            DomainAnnotations = new Dictionary<string, string>();
            LinkResolverPlugins = new List<PluginConfig>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
