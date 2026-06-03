using System.Collections.Generic;

namespace Foxtension.Firmware.NIC
{
    public sealed class NicResult
    {
        public List<(string Address, string AddressFamily, bool IsIPv4MappedToIPv6, bool IsIPv6LinkLocal, bool IsIPv6Multicast, bool IsIPv6SiteLocal, bool IsIPv6Teredo, bool IsIPv6UniqueLocal, long ScopeId)> UnicastAddressesAddress { get; set; } = new();


        public List<(string IpAddress, string NetMask, string Scope)> IpAddresses = new();
        public List<string> DnsAddresses = new();
        public List<string> GatewayAddresses = new();
        public List<string> MulticastAddresses = new();
        public List<string> AnycastAddresses = new();
        public List<string> UnicastAddresses = new();
        public List<string> DhcpServerAddresses = new();
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? NetworkInterfaceType { get; set; }
        public string? OperationalStatus { get; set; }
        public string? MacAddress { get; set; }
        public long SpeedBitsPerSecond { get; set; } = 0;
        public bool SupportsIpv4 { get; set; }
        public bool SupportsIpv6 { get; set; }
        public bool SupportMulticast { get; set; }
        public string? DnsSuffix { get; set; }
        public int Mtu { get; set; } = -1;
        public int Ipv4Index { get; set; } = -1;
        public string[]? DnsList { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public long UnicastPacketsSent { get; set; }
        public long UnicastPacketsReceived { get; set; }
        public long NonUnicastPacketsReceived { get; set; }
        public long IncomingPacketsDiscarded { get; set; }
        public long IncomingPacketsWithErrors { get; set; }
        public long OutgoingPacketsWithErrors { get; set; }
    }
}