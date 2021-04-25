using System.Numerics;

namespace SharpIrcBot.Plugins.Stats
{
    public struct VaccinationStatsFields
    {
        public BigInteger Vaccinations;
        public BigInteger PartiallyImmune;
        public BigInteger FullyImmune;
    }
}
