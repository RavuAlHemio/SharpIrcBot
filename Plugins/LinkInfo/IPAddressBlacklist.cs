using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SharpIrcBot.Plugins.LinkInfo
{
    static class IPAddressBlacklist
    {
        public static bool IsIPAddressBlacklisted(IPAddress address)
        {
            var addressBytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                if (addressBytes[0] == 0)
                {
                    // "this network" 0.0.0.0/8
                    return true;
                }
                else if (addressBytes[0] == 10)
                {
                    // private 10.0.0.0/8
                    return true;
                }
                else if (addressBytes[0] == 100 && (addressBytes[1] & 0xC0) == 64)
                {
                    // Shared Address Space 100.64.0.0/10
                    return true;
                }
                else if (addressBytes[0] == 127)
                {
                    // loopback 127.0.0.0/8
                    return true;
                }
                else if (addressBytes[0] == 169 && addressBytes[1] == 254)
                {
                    // link-local 169.254.0.0/16
                    return true;
                }
                else if (addressBytes[0] == 172 && (addressBytes[1] & 0xF0) == 0x10)
                {
                    // private 172.16.0.0/12
                    return true;
                }
                else if (addressBytes[0] == 192 && addressBytes[1] == 0 && addressBytes[2] == 0)
                {
                    // IETF protocol assignments 192.0.0.0/24
                    return true;
                }
                else if (addressBytes[0] == 192 && addressBytes[1] == 0 && addressBytes[2] == 2)
                {
                    // TEST-NET-1 192.0.2.0/24
                    return true;
                }
                else if (addressBytes[0] == 192 && addressBytes[1] == 168)
                {
                    // private 192.168.0.0/16
                    return true;
                }
                else if (addressBytes[0] == 198 && (addressBytes[1] & 0xFE) == 18)
                {
                    // benchmark tests 198.18.0.0/15
                    return true;
                }
                else if (addressBytes[0] == 198 && addressBytes[1] == 51 && addressBytes[2] == 100)
                {
                    // TEST-NET-2 198.51.100.0/24
                    return true;
                }
                else if (addressBytes[0] == 203 && addressBytes[1] == 0 && addressBytes[2] == 113)
                {
                    // TEST-NET-3 203.0.113.0/24
                    return true;
                }
                else if ((addressBytes[0] & 0xF0) == 224)
                {
                    // multicast 224.0.0.0/4
                    return true;
                }
                else if ((addressBytes[0] & 0xF0) == 240)
                {
                    // reserved 240.0.0.0/4
                    return true;
                }
                else if (addressBytes[0] == 255 && addressBytes[1] == 255 && addressBytes[2] == 255 &&
                         addressBytes[3] == 255)
                {
                    // broadcast 255.255.255.255/32
                    return true;
                }

                return false;
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (addressBytes.Take(15).All(b => b == 0))
                {
                    if (addressBytes[15] == 0)
                    {
                        // unspecified address ::/128
                        return true;
                    }
                    else if (addressBytes[15] == 1)
                    {
                        // loopback ::1/128
                        return true;
                    }
                }
                else if (addressBytes[0] == 0x01 && addressBytes.Skip(1).Take(7).All(b => b == 0))
                {
                    // Discard-Only Address Block 100::/64
                    return true;
                }
                else if (addressBytes[0] == 0x20 && addressBytes[1] == 0x01)
                {
                    if ((addressBytes[2] & 0xFE) == 0)
                    {
                        // IETF Protocol Assignments 2001::/23
                        return true;
                    }
                    else if (addressBytes[2] == 0x00 && addressBytes[3] == 0x02 && addressBytes[4] == 0x00 && addressBytes[5] == 0x00)
                    {
                        // Benchmarking 2001:2::/48
                        return true;
                    }
                    else if (addressBytes[2] == 0x0D && addressBytes[3] == 0xB8)
                    {
                        // documentation 2001:db8::/32
                        return true;
                    }
                    else if (addressBytes[2] == 0x00 && (addressBytes[3] & 0xF0) == 0x10)
                    {
                        // deprecated (previously ORCHID) 2001:10::/28
                        return true;
                    }
                    else if (addressBytes[2] == 0x00 && (addressBytes[3] & 0xF0) == 0x20)
                    {
                        // ORCHIDv2 2001:20::/28
                        return true;
                    }
                }
                else if ((addressBytes[0] & 0xFE) == 0xFC)
                {
                    // Unique-Local fc00::/7
                    return true;
                }
                else if (addressBytes[0] == 0xFE && (addressBytes[0] & 0xC0) == 0x80)
                {
                    // Linked-Scoped Unicast fe80::/10
                    return true;
                }

                return false;
            }

            // assume good faith for the other protocol types -- nobody uses them anyway
            return false;
        }

    }
}
