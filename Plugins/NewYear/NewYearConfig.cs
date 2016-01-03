using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NewYear
{
    [JsonObject(MemberSerialization.OptOut)]
    public class NewYearConfig
    {
        public bool LocalTime { get; set; }
        public int CustomMonth { get; set; }
        public int CustomDay { get; set; }
        public int CustomHour { get; set; }
        public int CustomMinute { get; set; }
        public int CustomSecond { get; set; }
        public long YearBiasToGregorian { get; set; }
        public HashSet<string> Channels { get; set; }

        public NewYearConfig(JObject obj)
        {
            LocalTime = true;
            CustomMonth = 1;
            CustomDay = 1;
            CustomHour = 0;
            CustomMinute = 0;
            CustomSecond = 0;
            YearBiasToGregorian = 0;
            Channels = new HashSet<string>();

            var ser = new JsonSerializer();
            ser.Populate(obj.CreateReader(), this);
        }
    }
}
