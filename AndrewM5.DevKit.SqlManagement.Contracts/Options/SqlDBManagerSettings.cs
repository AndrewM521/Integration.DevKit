using System.Collections.Concurrent;

namespace AndrewM5.DevKit.SqlManagement.Abstractions.Options;

public class SqlDBManagerSettings
{
    public int Default_CommandTimeout_Seconds { get; set; } = 30;

    public ConcurrentDictionary<string, SqlDBClientSettings> Clients { get; set; } = new ConcurrentDictionary<string, SqlDBClientSettings>();

    public SqlDBManagerSettings Clone()
    {
        return new SqlDBManagerSettings
        {
            Clients = this.Clients
        };
    }
}
