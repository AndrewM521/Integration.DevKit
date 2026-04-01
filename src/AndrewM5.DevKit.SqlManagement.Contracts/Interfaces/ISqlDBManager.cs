using AndrewM5.DevKit.SqlManagement.Abstractions.Options;

namespace AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;

public interface ISqlDBManager : IAsyncDisposable
{
    public SqlDBManagerSettings RuntimeSettings { get; set; }

    public ISqlDBClient GetClient(string clientName);

    public void OutputRuntimeOptions();
}
