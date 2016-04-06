using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace AlsoKnownAs
{
    public class HostnameUserIdentifier : UserIdentifier
    {
        /// <remarks>
        /// Stored in big-endian format ("sharpircbot.ondrahosek.com" -> ["com", "ondrahosek", "sharpircbot"])
        /// </remarks>
        [NotNull, ItemNotNull]
        public ImmutableList<string> HostnameParts { get; }

        public HostnameUserIdentifier([NotNull, ItemNotNull] ImmutableList<string> hostnameParts)
        {
            HostnameParts = hostnameParts;
        }

        public HostnameUserIdentifier([NotNull, ItemNotNull] IEnumerable<string> hostnameParts)
        {
            HostnameParts = ImmutableList.CreateRange(hostnameParts);
        }

        public HostnameUserIdentifier([NotNull] string hostname)
        {
            HostnameParts = hostname.Split('.').Reverse().ToImmutableList();
        }

        public override bool Equals(UserIdentifier other)
        {
            var hui = other as HostnameUserIdentifier;
            if (hui != null)
            {
                return this.HostnameParts
                    .Zip(hui.HostnameParts, (t, o) => t.Equals(o))
                    .All(areEqual => areEqual);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HostnameParts.Aggregate(0, (current, part) => unchecked(current + part.GetHashCode()));
        }

        public override string ToString()
        {
            return string.Join(".", HostnameParts.Reverse());
        }
    }
}
