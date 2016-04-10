using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace AlsoKnownAs
{
    public class IPAddressUserIdentifier : UserIdentifier
    {
        [NotNull]
        public IPAddress Address { get; }

        public IPAddressUserIdentifier([NotNull] IPAddress address)
        {
            Address = address;
        }

        public IPAddressUserIdentifier([NotNull] string address)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(address, out ipAddress))
            {
                throw new FormatException("invalid IP address");
            }
            Address = ipAddress;
        }

        [CanBeNull]
        public static IPAddressUserIdentifier TryParse([NotNull] string address)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(address, out ipAddress))
            {
                return new IPAddressUserIdentifier(ipAddress);
            }
            return null;
        }

        public override bool Equals(UserIdentifier other)
        {
            var ipaui = other as IPAddressUserIdentifier;
            if (ipaui != null)
            {
                return this.Address.Equals(ipaui.Address);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }

        public override string ToString()
        {
            return Address.ToString();
        }

        public override ImmutableList<string> Parts
        {
            get
            {
                switch (Address.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        // IPv4
                        return Address.ToString().Split('.').ToImmutableList();
                    case AddressFamily.InterNetworkV6:
                        // IPv6
                        return Address.ToString().Split(':').ToImmutableList();
                    default:
                        throw new FormatException($"unexpected address family {Address.AddressFamily}");
                }
            }
        }
    }
}
