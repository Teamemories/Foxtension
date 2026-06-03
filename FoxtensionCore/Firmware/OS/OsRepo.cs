using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Foxtension.Firmware.OS
{
    public sealed class OsKnowledge
    {
        public OsResult Scan()
        {
            string? tmpOsName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                tmpOsName = "Windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                tmpOsName = "Linux Kernel";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                tmpOsName = "MacOS (OSX)";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                tmpOsName = "FreeBSD";
            else
                tmpOsName = "Unknown";

            var res = new OsResult
            {
                OSName = tmpOsName,
                OSVersion = Environment.OSVersion.Version.ToString() ?? "Unknown",
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString() ?? "Unknown",
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString() ?? "Unknown",
                MachineName = Environment.MachineName.ToString() ?? "Unknown",
                UserName = Environment.UserName.ToString() ?? "Unknown",
                UserDomainName = Environment.UserDomainName.ToString() ?? "Unknown",
                FrameworkDescription = RuntimeInformation.FrameworkDescription.ToString() ?? "Unknown",
                Platform = Environment.OSVersion.Platform.ToString() ?? "Unknown",
                ProcessorCount = Environment.ProcessorCount,
                Is64BitProcess = Environment.Is64BitProcess,
                Is64BitOS = Environment.Is64BitOperatingSystem,
                SystemDirectory = Environment.SystemDirectory ?? "Unknown",
                CurrentDirectory = Environment.CurrentDirectory ?? "Unknown",
                UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? "Unknown",
                TempPath = Path.GetTempPath() ?? "Unknown",
                WorkingSet = Process.GetCurrentProcess().WorkingSet64
            };

            return res;
        }
    }
}