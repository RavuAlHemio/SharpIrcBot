using System.Collections.Generic;
using Newtonsoft.Json;

namespace Punt
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PuntPattern
    {
        public List<string> NickPatterns { get; set; }
        public List<string> NickExceptPatterns { get; set; }
        public List<string> BodyPatterns { get; set; }
        public List<string> BodyExceptPatterns { get; set; }
        public string KickMessage { get; set; }
        public int? ChancePercent { get; set; }

        public PuntPattern()
        {
            NickPatterns = new List<string>();
            NickExceptPatterns = new List<string>();
            BodyPatterns = new List<string>();
            BodyExceptPatterns = new List<string>();
            KickMessage = null;
            ChancePercent = null;
        }
    }
}
