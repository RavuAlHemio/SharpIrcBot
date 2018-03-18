using System.Collections.Generic;
using Newtonsoft.Json;

namespace SharpIrcBot.Plugins.Debounce
{
    [JsonObject(MemberSerialization.OptOut)]
    public class BounceCriterion
    {
        public List<string> NicknameRegexes { get; set; }
        public List<string> ChannelRegexes { get; set; }
        public List<string> KickMessages { get; set; }
        public bool ForgetOnChannelMessage { get; set; }
        public int MaxJoinsQuitsInTimeSlice { get; set; }
        public double TimeSliceMinutes { get; set; }
        public double? StartingHour { get; set; }
        public double? EndingHour { get; set; }
        public double? BanDurationMinutes { get; set; }
    }
}
