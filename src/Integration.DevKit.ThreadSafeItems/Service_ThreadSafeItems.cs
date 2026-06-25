/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadLocks.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.DevKit.ThreadSafeItems;

/// <summary>
/// Provides static access to thread-safe service instances within the application.
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be initialized during application startup (e.g., in Program.cs or Startup.cs) 
/// after the service provider has been built.
/// </remarks>
public static class Service_ThreadSafeItems
{
    private static readonly IServiceCollection _internalServiceCollection = new ServiceCollection();
    private const string NoInit = "Service_ThreadSafeItems has not been initialized.";

    private static ThreadSafeFileIO? _threadSafeFileIO;

    /// <summary>
    /// Adds the <see cref="ThreadSafeFileIO"/> service to the service collection as a singleton.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance to support a fluent configuration syntax.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddThreadSafeItems(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<ThreadSafeFileIO>();

        return services;
    }

    /// <summary>
    /// Initializes the static host by resolving <see cref="ThreadSafeFileIO"/> and validating 
    /// its dependencies from the service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve dependencies.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="IThreadLockManager"/> or <see cref="ThreadSafeFileIO"/> are not 
    /// correctly registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        try
        {
            _ = sp.GetRequiredService<IThreadLockManager>();
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"{nameof(Service_ThreadSafeItems)} requires the ThreadLocks module. Call AddThreadLocks() before AddThreadSafeItems()");
        }

        _threadSafeFileIO = sp.GetService<ThreadSafeFileIO>();
        if (_threadSafeFileIO == null)
        {
            throw new InvalidOperationException($"{nameof(ThreadSafeFileIO)} is not registered. Make sure you call AddThreadSafeItems() when configuring services.");
        }
    }


    /// <summary>
    /// Initializes the static <see cref="ThreadSafeFileIOClass"/>.
    /// </summary>
    /// <remarks>
    /// This should only be used if your service provider is already built as this adds to an internal service collection. 
    /// </remarks>
    public static void Initialize_OnDemand()
    {
        _internalServiceCollection.AddThreadLocks();
        _internalServiceCollection.AddThreadSafeItems();

        var provider = _internalServiceCollection.BuildServiceProvider();

        _threadSafeFileIO = provider.GetRequiredService<ThreadSafeFileIO>();
    }

    /// <summary>
    /// Gets the globally accessible instance of the <see cref="ThreadSafeFileIO"/> class.
    /// </summary>
    /// <value>
    /// The initialized <see cref="ThreadSafeFileIO"/> instance used for thread-safe file operations.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the property is accessed before <see cref="Initialize(IServiceProvider)"/> has been called.
    /// </exception>
    public static ThreadSafeFileIO ThreadSafeFileIOClass { 
        get
        {
            if (_threadSafeFileIO == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _threadSafeFileIO;
        }
    }
}
