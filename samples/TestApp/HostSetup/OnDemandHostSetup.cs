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
/// The <see cref="OnDemandHost"/> alternative to <see cref="RegularHostSetup"/> — a static,
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
    public async Task StartAsync(IConfiguration decryptedConfig, SerilogLoggerSetup? serilogSetup = null)
    {
        OnDemandHost.ConfigureServices(services =>
        {
            if (serilogSetup != null)
            {
                serilogSetup.ApplyTo(services);
            }
            else
            {
                services.AddLogging(builder => builder.AddConsole());
            }

            services.AddProcessLauncher(decryptedConfig);
            services.AddRESTApiMgmt(decryptedConfig);
            services.AddCredentialMgmt(decryptedConfig);
            services.AddThreadLocks(decryptedConfig);
            services.AddThreadSafeItems(decryptedConfig);
            services.AddTaskMgmt(decryptedConfig);
            services.AddSQLMgmt(decryptedConfig);
        });

        await OnDemandHost.StartAsync(decryptedConfig);

        Service_ProcessLauncher.Initialize(OnDemandHost.Services);
        Service_RESTApiMgmt.Initialize(OnDemandHost.Services);
        Service_ThreadLocks.Initialize(OnDemandHost.Services);
        Service_ThreadSafeItems.Initialize(OnDemandHost.Services);
        Service_TaskMgmt.Initialize(OnDemandHost.Services);
        Service_SQLMgmt.Initialize(OnDemandHost.Services);
        Service_CredentialMgmt.InitializeFileSecretStore(OnDemandHost.Services);
    }

    public Task StopAsync() => OnDemandHost.StopAsync();
}
