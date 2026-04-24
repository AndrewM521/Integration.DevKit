using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.TaskManagement;

public sealed class ManagedTaskIterationSnapshot : IManagedTaskIterationSnapshot
{
    public int IterationNumber { get; internal set; }

    public ManagedTaskState State { get; internal set; }

    public DateTime StartDTM { get; internal set; }

    public DateTime EndDTM { get; internal set; }

    public TimeSpan Runtime { get; internal set; }

    public Exception? Exception { get; internal set; }

    internal ManagedTaskIterationSnapshot(ManagedTaskIterationRuntime runtime, Exception? ex = null)
    {
        IterationNumber = runtime.IterationNumber;
        State = runtime.State;
        StartDTM = runtime.StartDTM;
        EndDTM = runtime.EndDTM;
        Runtime = runtime.Runtime;
        Exception = ex;
    }

    public string GetIterationInfo(bool includeIndent = true)
    {
        string indent = "";

        if (includeIndent)
        {
            indent = "    ";
        }

        return $@"
        {indent}IterationNumber: {IterationNumber}        
        {indent}State: {State}        
        {indent}StartUtc: {StartDTM:yyyy-MM-dd HH:mm:ss.fff}
        {indent}EndUtc: {(EndDTM == DateTime.MinValue ? "N/A" : EndDTM.ToString("yyyy-MM-dd HH:mm:ss.fff"))}
        {indent}Runtime: {Runtime}
        {indent}ExceptionType: {Exception?.GetType().Name ?? "None"}
        {indent}ExceptionMessage: {Exception?.Message ?? "None"}
        ";
    }
}
