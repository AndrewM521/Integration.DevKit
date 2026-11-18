/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core.OnDemand;
using Integration.DevKit.CredentialMgmt;
using Integration.DevKit.ProcessLauncher;
using Integration.DevKit.RESTApiMgmt;
using Integration.DevKit.SQLMgmt;
using Integration.DevKit.TaskMgmt;
using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadSafeItems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestApp.HostSetup;

/// <summary>
/// The <see cref="OnDemandHost"/> alternative to <see cref="RegularHostSetup"/>. A lightweight,
/// non-<see cref="Microsoft.Extensions.Hosting.IHostBuilder"/> service container for non-DI/on-demand
/// scenarios. Registers the same DevKit modules and runs the same <c>Service_Xxx.Initialize</c> calls,
/// just through <see cref="OnDemandHost"/>'s own <c>ConfigureServices</c>/<c>StartAsync</c>/<c>StopAsync</c>.
/// </summary>
/// <remarks>
/// Defaults to the standard <see cref="Microsoft.Extensions.Logging"/> console provider — unlike
/// <see cref="RegularHostSetup"/>, <see cref="OnDemandHost"/> has no <see cref="Microsoft.Extensions.Hosting.IHostBuilder"/>
/// to inherit default logging from, so that has to be registered explicitly here. Pass a
/// <see cref="SerilogLoggerSetup"/> to <see cref="StartAsync"/> to opt into Serilog instead.
/// </remarks>
public class OnDemandHostSetup
{
    private readonly OnDemandHost _host = new OnDemandHost();

    public async Task StartAsync(IConfiguration configuration, SerilogLoggerSetup? serilogSetup = null)
    {
        _host.ConfigureServices(services =>
        {
            if (serilogSetup != null)
            {
                serilogSetup.ApplyTo(services);
            }
            else
            {
                services.AddLogging(builder => builder.AddConsole());
            }

            services.AddProcessLauncher(configuration);
            services.AddRESTApiMgmt(configuration);
            services.AddCredentialMgmt(configuration);
            services.AddThreadLocks(configuration);
            services.AddThreadSafeItems(configuration);
            services.AddTaskMgmt(configuration);
            services.AddSQLMgmt(configuration);
        });

        await _host.StartAsync(configuration);

        Service_ProcessLauncher.Initialize(_host.Services);
        Service_RESTApiMgmt.Initialize(_host.Services);
        Service_ThreadLocks.Initialize(_host.Services);
        Service_ThreadSafeItems.Initialize(_host.Services);
        Service_TaskMgmt.Initialize(_host.Services);
        Service_SQLMgmt.Initialize(_host.Services);
        Service_CredentialMgmt.InitializeFileSecretStore(_host.Services);
    }

    public Task StopAsync()
    {
        return _host.StopAsync();
    }
}
