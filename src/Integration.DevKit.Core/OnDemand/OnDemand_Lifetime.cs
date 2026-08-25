using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.Core.OnDemand;

/// <summary>
/// Provides a lightweight implementation of the host application lifetime for on-demand shutdown scenarios.
/// </summary>
public class OnDemand_Lifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();

    /// <summary>
    /// Gets a token that is triggered when the application has started.
    /// </summary>
    public CancellationToken ApplicationStarted => _startedSource.Token;

    /// <summary>
    /// Gets a token that is triggered when the application is stopping.
    /// </summary>
    public CancellationToken ApplicationStopping => _stoppingSource.Token;

    /// <summary>
    /// Gets a token that is triggered when the application has stopped.
    /// </summary>
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    /// <summary>
    /// Requests the application lifetime to stop and propagate the shutdown signal.
    /// </summary>
    public void StopApplication() => _stoppingSource.Cancel();

    // Call this manually in your Program.cs Console.CancelKeyPress event
    public void TriggerShutdown()
    {
        _stoppingSource.Cancel();
        _stoppedSource.Cancel();
    }
}
