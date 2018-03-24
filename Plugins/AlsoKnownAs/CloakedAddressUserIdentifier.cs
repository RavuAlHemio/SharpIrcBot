using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.AlsoKnownAs
{
    public class CloakedAddressUserIdentifier : UserIdentifier
    {
        /// <remarks>
        /// Stored in big-endian format ("ija.tft.vro4p6" -> ["vro4p6", "tft", "ija"])
        /// </remarks>
        [NotNull, ItemNotNull]
        public ImmutableList<string> CloakedAddressParts { get; }

        public CloakedAddressUserIdentifier([NotNull, ItemNotNull] ImmutableList<string> cloakedAddressParts)
        {
            CloakedAddressParts = cloakedAddressParts;
        }

        public CloakedAddressUserIdentifier([NotNull, ItemNotNull] IEnumerable<string> cloakedAddressParts)
        {
            CloakedAddressParts = ImmutableList.CreateRange(cloakedAddressParts);
        }

        public CloakedAddressUserIdentifier([NotNull] string cloakedAddress)
        {
            CloakedAddressParts = cloakedAddress.Split('.').Reverse().ToImmutableList();
        }

        public override bool Equals(UserIdentifier other)
        {
            var hui = other as HostnameUserIdentifier;
            if (hui != null)
            {
                return this.CloakedAddressParts
                    .Zip(hui.HostnameParts, (t, o) => t.Equals(o))
                    .All(areEqual => areEqual);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return CloakedAddressParts.Aggregate(0, (current, part) => unchecked(current + part.GetHashCode()));
        }

        public override string ToString()
        {
            return CloakedAddressParts
                .Reverse()
                .StringJoin(".");
        }

        public override ImmutableList<string> Parts => CloakedAddressParts;
    }
}
