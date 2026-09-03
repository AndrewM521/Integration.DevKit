/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.CredentialMgmt;
using Integration.DevKit.ProcessLauncher;
using Integration.DevKit.RESTApiMgmt;
using Integration.DevKit.SQLMgmt;
using Integration.DevKit.TaskMgmt;
using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadSafeItems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestApp.HostSetup;

/// <summary>
/// The regular, DI-container-backed <see cref="IHost"/> setup — registers every DevKit module and
/// initializes their <c>Service_Xxx</c> static facades.
/// </summary>
/// <remarks>
/// Defaults to the standard <see cref="Microsoft.Extensions.Logging"/> pipeline that
/// <see cref="Host.CreateDefaultBuilder(string[])"/> already wires up (Console/Debug/EventSource
/// providers) — pass a <see cref="SerilogLoggerSetup"/> to <see cref="StartAsync"/> to opt into
/// Serilog instead.
/// </remarks>
public class RegularHostSetup
{
    private IHost? _app;

    public async Task StartAsync(string[] args, IConfiguration decryptedConfig, SerilogLoggerSetup? serilogSetup = null)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args);

        if (serilogSetup != null)
        {
            hostBuilder = serilogSetup.ApplyTo(hostBuilder);
        }

        hostBuilder = hostBuilder
            .ConfigureServices((context, services) =>
            {
                services.AddProcessLauncher(decryptedConfig);
                services.AddRESTApiMgmt(decryptedConfig);
                // "File" is just the built-in preset in the credential-management provider registry
                // (read from the "Integration.DevKit:CredentialManagement" section of appsettings.json),
                // selected via AddCredentialMgmt like any other provider — including a custom one
                // registered via Service_CredentialMgmt.RegisterProvider.
                services.AddCredentialMgmt(decryptedConfig);
                services.AddThreadLocks(decryptedConfig);
                services.AddThreadSafeItems(decryptedConfig);
                services.AddTaskMgmt(decryptedConfig);
                services.AddSQLMgmt(decryptedConfig);
            });

        _app = hostBuilder.Build();

        Service_ProcessLauncher.Initialize(_app.Services);
        Service_RESTApiMgmt.Initialize(_app.Services);
        Service_ThreadLocks.Initialize(_app.Services);
        Service_ThreadSafeItems.Initialize(_app.Services);
        Service_TaskMgmt.Initialize(_app.Services);
        Service_SQLMgmt.Initialize(_app.Services);
        Service_CredentialMgmt.InitializeFileSecretStore(_app.Services);

        await _app.StartAsync();
    }

    public Task StopAsync() => _app?.StopAsync() ?? Task.CompletedTask;
}
