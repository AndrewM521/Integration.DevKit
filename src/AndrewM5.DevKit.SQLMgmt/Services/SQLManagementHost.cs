/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.SQLMgmt.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.SQLMgmt.Services;

/// <summary>
/// Provides a static entry point to access the SqlClientManagement module
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be initialized during application startup (e.g., in Program.cs or Startup.cs) 
/// after the service provider has been built.
/// </remarks>
public class SQLManagementHost
{
    private const string NoInit = "SqlManagementHost has not been initialized.";

    private static ISQLManager? _sqlManager;

    /// <summary>
    /// Initializes the static host with a service provider to resolve the <see cref="ISQLManager"/>.
    /// </summary>
    /// <param name="sp">The service provider containing the registered SQL management services.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="sp"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="ISQLManager"/> is not registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _sqlManager = sp.GetService<ISQLManager>();
        if (_sqlManager == null)
        {
            throw new InvalidOperationException($"{nameof(ISQLManager)} is not registered. Make sure you call AddSqlDBManagement() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="ISQLManager"/> for the current process.
    /// </summary>
    /// <value>
    /// The global <see cref="ISQLManager"/> instance used to orchestrate database clients.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the host has not been initialized via <see cref="Initialize"/>.
    /// </exception>
    public static ISQLManager SQLManager
    {
        get
        {
            if (_sqlManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _sqlManager;
        }
    }
}
