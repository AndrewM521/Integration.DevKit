using Microsoft.Extensions.Hosting;

namespace Integration.DevKit.Core.OnDemand;

/// <summary>
/// Minimal <see cref="IHostApplicationLifetime"/> for <see cref="OnDemandHost"/>, so modules that need one
/// (such as Task Management) work without a generic host.
/// </summary>
/// <remarks>
/// A cancellation token can only be canceled once, so <see cref="OnDemandHost"/> creates a new lifetime on every start.
/// </remarks>
internal sealed class OnDemand_Lifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();

    /// <inheritdoc />
    public CancellationToken ApplicationStarted => _startedSource.Token;

    /// <inheritdoc />
    public CancellationToken ApplicationStopping => _stoppingSource.Token;

    /// <inheritdoc />
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    /// <summary>
    /// Signals <see cref="ApplicationStopping"/>. Hosted services are stopped by <see cref="OnDemandHost.StopAsync"/>.
    /// </summary>
    public void StopApplication()
    {
        _stoppingSource.Cancel();
    }

    /// <summary>
    /// Signals <see cref="ApplicationStarted"/>.
    /// </summary>
    internal void NotifyStarted()
    {
        _startedSource.Cancel();
    }

    /// <summary>
    /// Signals <see cref="ApplicationStopping"/>.
    /// </summary>
    internal void NotifyStopping()
    {
        _stoppingSource.Cancel();
    }

    /// <summary>
    /// Signals <see cref="ApplicationStopped"/>.
    /// </summary>
    internal void NotifyStopped()
    {
        _stoppedSource.Cancel();
    }
}
