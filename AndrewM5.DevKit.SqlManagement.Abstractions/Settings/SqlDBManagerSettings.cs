using System.Collections.Concurrent;

namespace AndrewM5.DevKit.SqlManagement.Abstractions.Settings;

public class SqlDBManagerSettings
{
    public int MaxSqlDBClients { get; set; } = 1;

    public int DefaultCommandTimeoutSeconds { get; set; } = 30;

    public ConcurrentDictionary<string, SqlDBClientSettings> Clients { get; set; } = new ConcurrentDictionary<string, SqlDBClientSettings>();

    public SqlDBManagerSettings Clone()
    {
        return new SqlDBManagerSettings
        {
            MaxSqlDBClients = this.MaxSqlDBClients,
            Clients = this.Clients
        };
    }
}
