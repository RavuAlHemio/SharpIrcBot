using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Punt
{
    [JsonObject(MemberSerialization.OptOut)]
    public struct PuntPattern
    {
        [JsonIgnore] public Regex NickPattern { get; set; }
        [JsonIgnore] public Regex BodyPattern { get; set; }
        public string KickMessage { get; set; }

        [JsonProperty("NickPattern")]
        public string NickPatternString
        {
            get { return NickPattern.ToString(); }
            set { NickPattern = new Regex(value, RegexOptions.Compiled); }
        }

        [JsonProperty("BodyPattern")]
        public string BodyPatternString
        {
            get { return NickPattern.ToString(); }
            set { NickPattern = new Regex(value, RegexOptions.Compiled); }
        }

        public int? ChancePercent { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PuntPattern && this == (PuntPattern)obj;
        }

        public override int GetHashCode()
        {
            return 3 * (NickPattern?.ToString().GetHashCode() ?? 0)
                + 5 * (BodyPattern?.ToString().GetHashCode() ?? 0)
                + 7 * (KickMessage?.GetHashCode() ?? 0)
                + 11 * ChancePercent.GetHashCode();
        }

        public static bool operator ==(PuntPattern x, PuntPattern y)
        {
            return x.NickPattern.ToString() == y.NickPattern.ToString()
                && x.BodyPattern.ToString() == y.BodyPattern.ToString()
                && x.KickMessage == y.KickMessage
                && x.ChancePercent == y.ChancePercent;
        }

        public static bool operator !=(PuntPattern x, PuntPattern y)
        {
            return !(x == y);
        }
    }
}
