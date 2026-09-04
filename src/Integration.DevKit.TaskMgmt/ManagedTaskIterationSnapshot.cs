/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Interfaces;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// Concrete Implementation of <see cref="IManagedTaskIterationSnapshot"/> representing a static, 
/// read-only snapshot of a managed task iteration's state at a specific point in time.
/// </summary>
public sealed class ManagedTaskIterationSnapshot : IManagedTaskIterationSnapshot
{
    /// <inheritdoc/>
    public int IterationNumber { get; internal set; }

    /// <inheritdoc/>
    public ManagedTaskState State { get; internal set; }

    /// <inheritdoc/>
    public DateTime StartDTM { get; internal set; }

    /// <inheritdoc/>
    public DateTime EndDTM { get; internal set; }

    /// <inheritdoc/>
    public TimeSpan Runtime { get; internal set; }

    /// <inheritdoc/>
    public Exception? Exception { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskIterationSnapshot"/> class 
    /// by capturing the current values from a <see cref="ManagedTaskIterationRuntime"/>.
    /// </summary>
    /// <param name="runtime">The live runtime to snapshot.</param>
    /// <param name="ex">An optional exception to associate with this snapshot.</param>
    internal ManagedTaskIterationSnapshot(ManagedTaskIterationRuntime runtime, Exception? ex = null)
    {
        IterationNumber = runtime.IterationNumber;
        State = runtime.State;
        StartDTM = runtime.StartDTM;
        EndDTM = runtime.EndDTM;
        Runtime = runtime.Runtime;
        Exception = ex;
    }

    /// <summary>
    /// Returns a formatted string containing detailed information about the iteration.
    /// </summary>
    /// <param name="includeIndent">If set to <c>true</c>, prefixes each line with a standard four-space indent.</param>
    /// <returns>A multiline string representation of the iteration metrics and state.</returns>
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
