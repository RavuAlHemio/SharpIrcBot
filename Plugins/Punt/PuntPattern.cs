using Newtonsoft.Json;

namespace Punt
{
    [JsonObject(MemberSerialization.OptOut)]
    public struct PuntPattern
    {
        public string NickPattern { get; set; }
        public string BodyPattern { get; set; }
        public string KickMessage { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PuntPattern && this == (PuntPattern)obj;
        }

        public override int GetHashCode()
        {
            return 3 * (NickPattern?.GetHashCode() ?? 0)
                + 5 * (BodyPattern?.GetHashCode() ?? 0)
                + 7 * (KickMessage?.GetHashCode() ?? 0);
        }

        public static bool operator ==(PuntPattern x, PuntPattern y)
        {
            return x.NickPattern == y.NickPattern
                && x.BodyPattern == y.BodyPattern
                && x.KickMessage == y.KickMessage;
        }

        public static bool operator !=(PuntPattern x, PuntPattern y)
        {
            return !(x == y);
        }
    }
}
