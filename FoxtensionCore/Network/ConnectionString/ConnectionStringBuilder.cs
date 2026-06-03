using System;
using System.Collections.Generic;
using System.IO;

namespace Foxtension.Network.ConnectionString
{
    public enum StringBuilderType
    {
        SqlServer,
        Sqlite,
        Ftp
    }
    public sealed class ConnectionStringBuilder
    {
        public StringBuilderType TargetType { get; set; }
        public string DataSource { get; set; } = string.Empty;
        public string ServerAddress { get; set; } = string.Empty;
        public int ServerPort { get; set; } = 0;
        public string DatabaseName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IntegratedSecurity { get; set; } = true;
        public bool TrustServerCertificate { get; set; } = true;
        public bool PersistSecurityInfo { get; set; } = false;
        public bool MultipleActiveResultSets { get; set; } = false;
        public bool Encryption { get; set; } = true;
        public bool Pooling { get; set; } = false;
        public string TargetPath { get; set; } = string.Empty;

        public string Build()
        {
            string cstring = string.Empty;
            var dic = new Dictionary<bool, string>
            {
                {true, "True" },
                {false, "False"}
            };
            switch (TargetType)
            {
                case StringBuilderType.SqlServer:
                    if (string.IsNullOrWhiteSpace(DatabaseName))
                        throw new ArgumentNullException("Database name cannot be empty.");
                    if (ServerPort < 0)
                        throw new ArgumentOutOfRangeException($"ServerPort is incorrect.");

                    cstring =
                        (!string.IsNullOrWhiteSpace(DataSource) ? $"Data Source={DataSource};" : string.Empty) +
                        (!string.IsNullOrWhiteSpace(ServerAddress) ? $"Server={ServerAddress};" : string.Empty) +
                        (ServerPort == 0 ? string.Empty : $"Port={ServerPort.ToString()};") +
                        $"Database={DatabaseName};" +
                        $"Integrated Security={dic[IntegratedSecurity]};" +
                        $"Persist Security Info={dic[PersistSecurityInfo]};" +
                        $"Pooling={dic[Pooling]};" +
                        $"MultipleActiveResultSets={dic[MultipleActiveResultSets]};" +
                        $"Encrypt={dic[Encryption]};" +
                        $"TrustServerCertificate={dic[TrustServerCertificate]};";

                    if (!IntegratedSecurity)
                    {
                        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                            throw new ArgumentNullException(nameof(ConnectionStringBuilder));

                        cstring += $"User Id={Username};Password={Password};";
                    }
                    break;
                case StringBuilderType.Sqlite:
                    if (string.IsNullOrWhiteSpace(DatabaseName) || string.IsNullOrWhiteSpace(TargetPath))
                        throw new ArgumentNullException(nameof(ConnectionStringBuilder));

                    cstring = $"Data Source={Path.Combine(TargetPath, $"{DatabaseName}.sqlite")}";
                    break;
                case StringBuilderType.Ftp:
                    if (string.IsNullOrWhiteSpace(ServerAddress) || string.IsNullOrWhiteSpace(Username))
                        throw new ArgumentNullException(nameof(ConnectionStringBuilder));

                    cstring = $"ftp://{Username}";
                    if (!string.IsNullOrWhiteSpace(Password))
                        cstring += $":{Uri.EscapeDataString(Password)}";
                    cstring += $"@{ServerAddress}";
                    if (ServerPort > 0)
                        cstring += $":{ServerPort}";
                    break;
                default:
                    throw new ArgumentException($"TargetType cannot be undecided.");
            }
            return cstring;
        }
    }
}