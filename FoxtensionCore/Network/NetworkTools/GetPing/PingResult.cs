using System.Net;
using System.Net.NetworkInformation;

namespace Foxtension.Network.NetworkTools.GetPing
{
    public sealed class PingResult
    {
        public bool Success { get; set; }
        public long Latency { get; set; }
        public double LatencyAverage { get; set; }
        public long MinLatency { get; set; }
        public long MaxLatency { get; set; }
        public long Jitter { get; set; }
        public double JitterAverage { get; set; }
        public int Sent { get; set; }
        public int Received { get; set; }
        public int Lost => Sent - Received;
        public double LossPercent => Lost * 100.0 / Sent;
        public IPAddress? Address { get; set; }
        public IPStatus Status { get; set; }
        public string? Message { get; set; }
    }
}
