using Integration.DevKit.Core.OnDemand;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Integration.DevKit.Core.Tests;

public class OnDemandHostTests
{
    private sealed class CountingService : IHostedService
    {
        public static int Starts;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Starts);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class DisposableSingleton : IDisposable
    {
        public bool Disposed;

        public void Dispose()
        {
            Disposed = true;
        }
    }

    [Fact]
    public async Task StartStopStart_GivesAFreshLifetimeAndDisposesTheOldProvider()
    {
        // Stop used to cancel a static lifetime for good, so a second Start handed out an already-canceled token
        CountingService.Starts = 0;
        await using var host = new OnDemandHost();
        host.ConfigureServices(services =>
        {
            services.AddHostedService<CountingService>();
            services.AddSingleton<DisposableSingleton>();
        });

        await host.StartAsync();
        var firstLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var firstSingleton = host.Services.GetRequiredService<DisposableSingleton>();
        Assert.True(firstLifetime.ApplicationStarted.IsCancellationRequested);

        await host.StopAsync();
        Assert.True(firstLifetime.ApplicationStopping.IsCancellationRequested);
        Assert.True(firstLifetime.ApplicationStopped.IsCancellationRequested);
        Assert.True(firstSingleton.Disposed);
        Assert.False(host.IsRunning);
        Assert.Throws<InvalidOperationException>(() => host.Services);

        await host.StartAsync();
        var secondLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        Assert.NotSame(firstLifetime, secondLifetime);
        Assert.False(secondLifetime.ApplicationStopping.IsCancellationRequested);
        Assert.Equal(2, CountingService.Starts);
    }

    [Fact]
    public async Task TwoHosts_AreIndependent()
    {
        await using var a = new OnDemandHost();
        await using var b = new OnDemandHost();
        a.ConfigureServices(services => services.AddSingleton<DisposableSingleton>());

        await a.StartAsync();
        await b.StartAsync();

        Assert.NotNull(a.Services.GetService<DisposableSingleton>());
        Assert.Null(b.Services.GetService<DisposableSingleton>());
    }

    private sealed class OrderedService : IHostedService
    {
        private readonly string _name;
        private readonly List<string> _log;
        private readonly int _startDelayMs;

        public OrderedService(string name, List<string> log, int startDelayMs = 0)
        {
            _name = name;
            _log = log;
            _startDelayMs = startDelayMs;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(_startDelayMs);
            lock (_log) { _log.Add("start " + _name); }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            lock (_log) { _log.Add("stop " + _name); }
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task HostedServices_StartInOrder_AndStopInReverse()
    {
        // Services used to start all at once, so a slow first service finished after the second
        var log = new List<string>();
        await using var host = new OnDemandHost();
        host.ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(new OrderedService("a", log, startDelayMs: 100));
            services.AddSingleton<IHostedService>(new OrderedService("b", log));
        });

        await host.StartAsync();
        await host.StopAsync();

        Assert.Equal(new[] { "start a", "start b", "stop b", "stop a" }, log);
    }

    private sealed class StopProbe : IHostedService
    {
        public readonly TaskCompletionSource Stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stopped.TrySetResult();
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task StopApplication_StopsTheHost()
    {
        // It used to only cancel ApplicationStopping: services kept running and IsRunning stayed true
        var probe = new StopProbe();
        await using var host = new OnDemandHost().ConfigureServices(s => s.AddSingleton<IHostedService>(probe));
        await host.StartAsync();

        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        Assert.Same(probe.Stopped.Task, await Task.WhenAny(probe.Stopped.Task, Task.Delay(5000)));
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (host.IsRunning && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.False(host.IsRunning);
    }
}
