using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LinkInfo
{
    [JsonObject(MemberSerialization.OptOut)]
    public class LinkInfoConfig
    {
        public long MaxDownloadSizeBytes { get; set; }
        public double TimeoutSeconds { get; set; }
        public double ImageInfoTimeoutSeconds { get; set; }
        public string FakeUserAgent { get; set; }
        public string GoogleDomain { get; set; }

        public LinkInfoConfig(JObject obj)
        {
            MaxDownloadSizeBytes = 10 * 1024 * 1024;
            TimeoutSeconds = 5.0;
            ImageInfoTimeoutSeconds = 1.0;
            FakeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:40.0) Gecko/20100101 Firefox/40.0";
            GoogleDomain = "www.google.at";

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
