using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LinkInfo
{
    [JsonObject(MemberSerialization.OptOut)]
    public class LinkInfoConfig
    {
        public long MaxDownloadSizeBytes { get; set; }

        public LinkInfoConfig(JObject obj)
        {
            MaxDownloadSizeBytes = 10 * 1024 * 1024;

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
