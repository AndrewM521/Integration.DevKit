using AndrewM5.DevKit.SqlManagement.Abstractions.Settings;

namespace AndrewM5.DevKit.SqlManagement.Abstractions;

public interface ISqlDBManager : IAsyncDisposable
{
    public SqlDBManagerSettings RuntimeSettings { get; set; }

    public ISqlDBClient GetClient(string clientName);

    public void OutputRuntimeOptions();
}
