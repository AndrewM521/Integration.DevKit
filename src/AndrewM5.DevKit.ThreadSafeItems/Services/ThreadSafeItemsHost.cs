/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadSafeItems.Services;

/// <summary>
/// Provides static access to thread-safe service instances within the application.
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be initialized during application startup (e.g., in Program.cs or Startup.cs) 
/// after the service provider has been built.
/// </remarks>
public static class ThreadSafeItemsHost
{
    private const string NoInit = "ThreadSafeItemsHost has not been initialized.";

    private static ThreadSafeFileIO? _threadSafeFileIO;

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
            throw new InvalidOperationException($"{nameof(ThreadSafeItemsServiceCollection)} requires the ThreadLocks module. Call AddThreadLocks() before AddThreadSafeItems()");
        }

        _threadSafeFileIO = sp.GetService<ThreadSafeFileIO>();
        if (_threadSafeFileIO == null)
        {
            throw new InvalidOperationException($"{nameof(ThreadSafeFileIO)} is not registered. Make sure you call AddThreadSafeItems() when configuring services.");
        }
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
