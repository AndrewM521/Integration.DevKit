/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;

namespace Integration.DevKit.RESTApiMgmt.Interfaces;

/// <summary>
/// Defines a pluggable authentication mechanism that can be applied to an outgoing
/// <see cref="HttpRequestMessage"/> before it is sent by an <see cref="ApiClient"/>.
/// </summary>
public interface IAuthStrategy
{
    /// <summary>
    /// Applies authentication to the given request (e.g. by setting its <c>Authorization</c> header),
    /// acquiring or refreshing any underlying credential as needed.
    /// </summary>
    /// <param name="request">The outgoing request to authenticate, mutated in place.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public Task<NullOperationResult> ApplyAsync(HttpRequestMessage request);
}
