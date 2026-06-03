using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Foxtension.Firmware.NIC
{
    public sealed class NicRepo
    {
        public List<NicResult> Scan()
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            
            var list = new List<NicResult>();

            foreach (var adapter in adapters)
            {
                try
                {
                    var props = adapter.GetIPProperties();
                    var stats = adapter.GetIPStatistics();

                    var ipv4 = props.GetIPv4Properties();

                    var unicasts = props.UnicastAddresses?.ToList() ?? new List<UnicastIPAddressInformation>();

                    var ips = new List<(string Address, string NetMask, string Scope)>();
                    foreach (var ua in unicasts)
                    {
                        string netmask = "Unknown";
                        if (ua.IPv4Mask != null)
                            netmask = ua.IPv4Mask!.ToString();
                        else if (ua.PrefixLength >= 0)
                            netmask = ua.PrefixLength.ToString();

                        ips.Add((ua.Address?.ToString() ?? "Unknown", netmask, ua.Address?.AddressFamily.ToString() ?? "Unknown"));
                    }

                    list.Add(new NicResult
                    {
                        Id = adapter.Id ?? "Unknown",
                        Name = adapter.Name ?? "Unknown",
                        Description = adapter.Description ?? "Unknown",
                        NetworkInterfaceType = adapter.NetworkInterfaceType.ToString() ?? "Unknown",
                        OperationalStatus = adapter.OperationalStatus.ToString() ?? "Unknown",
                        MacAddress = adapter.GetPhysicalAddress().ToString() ?? "Unknown",
                        SpeedBitsPerSecond = adapter.Speed <= 0 ? -1L : adapter.Speed,
                        SupportsIpv4 = adapter.Supports(NetworkInterfaceComponent.IPv4),
                        SupportsIpv6 = adapter.Supports(NetworkInterfaceComponent.IPv6),
                        SupportMulticast = adapter.SupportsMulticast,
                        DnsSuffix = props.DnsSuffix ?? "Unknown",
                        Mtu = ipv4?.Mtu ?? -1,
                        Ipv4Index = ipv4?.Index ?? -1,
                        IpAddresses = ips,
                        BytesSent = stats.BytesSent <= 0 ? -1L : stats.BytesSent,
                        BytesReceived = stats.BytesReceived <= 0 ? -1L : stats.BytesReceived,
                        UnicastPacketsSent = stats.UnicastPacketsSent <= 0 ? -1L : stats.UnicastPacketsSent,
                        UnicastPacketsReceived = stats.UnicastPacketsReceived <= 0 ? -1L : stats.UnicastPacketsReceived,
                        NonUnicastPacketsReceived = stats.NonUnicastPacketsReceived <= 0 ? -1L : stats.NonUnicastPacketsReceived,
                        IncomingPacketsDiscarded = stats.IncomingPacketsDiscarded <= 0 ? -1L : stats.IncomingPacketsDiscarded,
                        IncomingPacketsWithErrors = stats.IncomingPacketsWithErrors <= 0 ? -1L : stats.IncomingPacketsWithErrors,
                        OutgoingPacketsWithErrors = stats.OutgoingPacketsWithErrors <= 0 ? -1L : stats.OutgoingPacketsWithErrors
                    });
                }
                catch (Exception) { continue; }
            }
            return list;
        }
    }
}