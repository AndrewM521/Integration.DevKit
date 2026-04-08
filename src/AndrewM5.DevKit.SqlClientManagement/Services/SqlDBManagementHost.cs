using AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.SqlManagement.Services;

/// <summary>
/// Provides a static entry point to access the <see cref="ISqlDBManager"/>. 
/// This class must be initialized during application startup to resolve the required services.
/// </summary>
public class SqlDBManagementHost
{
    private const string NoInit = "SqlManagementHost has not been initialized.";

    private static ISqlDBManager? _sqlManager;

    /// <summary>
    /// Initializes the static host with a service provider to resolve the <see cref="ISqlDBManager"/>.
    /// </summary>
    /// <param name="sp">The service provider containing the registered SQL management services.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="ISqlDBManager"/> is not registered in the service collection.</exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _sqlManager = sp.GetService<ISqlDBManager>();
        if (_sqlManager == null)
        {
            throw new InvalidOperationException($"{nameof(ISqlDBManager)} is not registered. Make sure you call AddSqlDBManagement() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="ISqlDBManager"/>.
    /// </summary>
    /// <value>The current SQL Database Manager instance.</value>
    /// <exception cref="InvalidOperationException">Thrown if the host has not been initialized via <see cref="Initialize"/>.</exception>
    public static ISqlDBManager ProcessManager
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
