using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Foxtension.Network.NetworkTools.GetPing
{
    public sealed class PingRepo : IDisposable, IAsyncDisposable
    {
        public CancellationToken? Cancellation { get; set; } = default;
        public string? TargetHost { get; set; } = null;
        public int Count { get; set; } = 4; // For unlimited run -> (Count < 0)
        public int TimeoutSeconds { get; set; } = 4;
        public int PacketSize { get; set; } = 32;
        public int TimeToLive { get; set; } = 128;
        public bool Fragment { get; set; } = true;
        public bool ForceIPv4 { get; set; } = false;
        public bool ForceIPv6 { get; set; } = false;

        private Ping? _ping;

        public PingRepo()
        {
            _ping = new Ping();
        }

        public async Task<PingResult> StartAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetHost))
                throw new ArgumentException("TargetHost cannot be null or empty.");

            var addresses = await Dns.GetHostAddressesAsync(TargetHost);

            if (ForceIPv4)
                addresses = addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();

            if (ForceIPv6)
                addresses = addresses.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6).ToArray();

            if (addresses.Length == 0)
                throw new Exception("No valid IP address found for the selected IP version.");

            var buffer = Encoding.ASCII.GetBytes(new string('a', PacketSize));
            var pingOptions = new PingOptions(TimeToLive, !Fragment);

            PingResult? result = null;
            var latencies = new List<long>();
            var jitters = new List<long>();
            long? lastLatency = null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(Cancellation ?? CancellationToken.None);

            Console.CancelKeyPress += (sender, e) =>
            {
                cts.Cancel();
                e.Cancel = true;
            };

            int sentCount = 0;
            while (Count < 0 || sentCount < Count)
            {
                if (cts.Token.IsCancellationRequested)
                    break;

                var (pingResult, updatedLastLatency) = await SendPingAsync(_ping!, TargetHost, buffer, pingOptions, lastLatency);
                lastLatency = updatedLastLatency;

                if (pingResult.Success)
                {
                    latencies.Add(pingResult.Latency);
                    if (updatedLastLatency.HasValue && latencies.Count > 1)
                    {
                        var j = Math.Abs(pingResult.Latency - updatedLastLatency.Value);
                        jitters.Add(j);
                    }
                }

                pingResult.Sent = sentCount + 1;
                pingResult.Received = latencies.Count;
                pingResult.LatencyAverage = latencies.Count > 0 ? latencies.Average() : 0;
                pingResult.MinLatency = latencies.Count > 0 ? latencies.Min() : 0;
                pingResult.MaxLatency = latencies.Count > 0 ? latencies.Max() : 0;
                pingResult.JitterAverage = jitters.Count > 0 ? jitters.Average() : 0;

                result = pingResult;

                sentCount++;
                try { await Task.Delay(1000, cts.Token); } catch { break; }
            }

            return result!;
        }

        private async Task<(PingResult Result, long? LastLatency)> SendPingAsync(Ping ping, string host, byte[] buffer, PingOptions options, long? lastLatency)
        {
            bool success = false;
            long latency = 0, jitter = 0;
            IPAddress address = null!;
            IPStatus status = IPStatus.Unknown;
            string message = null!;
            long? newLastLatency = lastLatency;

            try
            {
                var reply = await ping.SendPingAsync(host, TimeoutSeconds * 1000, buffer, options);

                status = reply.Status;
                message = reply.Status.ToString();
                address = reply.Address;

                success = status == IPStatus.Success;
                if (success)
                {
                    latency = reply.RoundtripTime;
                    if (lastLatency.HasValue)
                        jitter = Math.Abs(latency - lastLatency.Value);

                    newLastLatency = latency;
                }
            }
            catch (Exception)
            {
                success = false;
                message = "Cannot connect to network!";
            }

            var result = new PingResult
            {
                Success = success,
                Latency = latency,
                Jitter = jitter,
                Address = address,
                Status = status,
                Message = message
            };

            return (result, newLastLatency);
        }

        #region Dispose
        public void Dispose()
        {
            _ping!.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}