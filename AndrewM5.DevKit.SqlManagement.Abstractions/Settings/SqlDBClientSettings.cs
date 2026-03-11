namespace AndrewM5.DevKit.SqlManagement.Abstractions.Settings;

public class SqlDBClientSettings
{
    public string Server { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool MultipleActiveResultSets { get; set; } = false;

    public bool TrustServerCertificate { get; set; } = true;

    public int ConnectionTimeout_Seconds { get; set; } = 15;

    public int? CommandTimeout_Seconds { get; set; }

    public bool UseSingleConnection { get; set; } = false;
}