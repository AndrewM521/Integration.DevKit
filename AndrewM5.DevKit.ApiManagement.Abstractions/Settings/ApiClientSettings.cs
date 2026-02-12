namespace AndrewM5.DevKit.ApiManagement.Abstractions.Settings;

public class ApiClientSettings
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "Unknown BaseURL";

    public int MaxConcurrentRequests { get; set; } = int.MaxValue;

    public Dictionary<string, string> DefaultHeaders { get; init; } = new Dictionary<string, string>();

    public int? TimeoutSeconds { get; set; } = null;
}
