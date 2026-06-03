namespace Foxtension.Firmware.OS
{
    public sealed class OsResult
    {
        public string? OSName { get; set; }
        public string? OSVersion { get; set; }
        public string? OSArchitecture { get; set; }
        public string? ProcessArchitecture { get; set; }
        public string? MachineName { get; set; }
        public string? UserName { get; set; }
        public string? UserDomainName { get; set; }
        public string? FrameworkDescription { get; set; }
        public string? Platform { get; set; }
        public int ProcessorCount { get; set; }
        public bool Is64BitProcess { get; set; }
        public bool Is64BitOS { get; set; }
        public string? SystemDirectory { get; set; }
        public string? CurrentDirectory { get; set; }
        public string? UserProfile { get; set; }
        public string? TempPath { get; set; }
        public long WorkingSet { get; set; }
    }
}