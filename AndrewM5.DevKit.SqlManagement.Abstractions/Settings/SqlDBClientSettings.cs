namespace AndrewM5.DevKit.SqlManagement.Abstractions.Settings;

public class SqlDBClientSettings
{
    public string Server { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool MultipleActiveResultSets { get; set; } = true;

    public bool TrustServerCertificate { get; set; } = true;

    public int MaxConcurrentQueries { get; set; } = int.MaxValue;

    public int ConnectionTimeoutSeconds { get; set; } = 15;

    public int? CommandTimeoutSeconds { get; set; }
}