/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace TestApp.HostSetup;

/// <summary>
/// Optional bring-your-own-logger setup: Serilog in place of the default
/// <see cref="Microsoft.Extensions.Logging"/> pipeline. DevKit
/// modules only depend on the standard <see cref="Microsoft.Extensions.Logging.ILogger"/>/<see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>
/// abstractions, so wiring Serilog in is all that's needed to make every DI-constructed module log
/// through it. Not applied unless a host setup is explicitly given an instance of this class — the
/// default is Microsoft's own logging (Console/Debug providers), which both host setups already get
/// for free without this.
/// </summary>
public class SerilogLoggerSetup
{
    /// <summary>
    /// Sets the global Serilog static logger. Call once, before building either host.
    /// </summary>
    public void ConfigureGlobalLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("C:\\NAS\\Home Drive\\Projects\\Junk\\.Keep\\serilog-log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    /// <summary>
    /// Wires Serilog into a <see cref="IHostBuilder"/> (the regular <c>Host</c> path).
    /// </summary>
    public IHostBuilder ApplyTo(IHostBuilder hostBuilder) => hostBuilder.UseSerilog();

    /// <summary>
    /// Wires Serilog into a raw <see cref="IServiceCollection"/> (the <c>OnDemandHost</c> path,
    /// which has no <see cref="IHostBuilder"/> to hang <see cref="ApplyTo(IHostBuilder)"/> off of).
    /// </summary>
    public void ApplyTo(IServiceCollection services) => services.AddLogging(builder => builder.AddSerilog());

    /// <summary>
    /// Flushes and closes the global Serilog logger. Call once, on the way out.
    /// </summary>
    public void Shutdown() => Log.CloseAndFlush();
}
