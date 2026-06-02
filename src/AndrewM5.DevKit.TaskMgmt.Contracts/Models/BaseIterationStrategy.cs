/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using AndrewM5.DevKit.TaskMgmt.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskMgmt.Contracts.Models;

/// <summary>
/// Concrete Implementation of <see cref="IIterationStrategy"/>
/// </summary>
public class BaseIterationStrategy : IIterationStrategy
{
    /// <inheritdoc/>
    /// <remarks>
    /// The base implementation returns <see cref="Task.CompletedTask"/> immediately. 
    /// It does not block or await any external triggers, effectively allowing the 
    /// execution engine to start the next task iteration immediately.
    /// </remarks>
    public virtual Task WaitForReadyAsync(IManagedTaskHandle handle, CancellationToken cancellationToken, ICustomLogger? logger = null)
    {
        return Task.CompletedTask;
    }
}
