// <copyright file="ILazy{T}.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lazy;

/// <summary>
/// Provides a mechanism for lazy initialization of a value.
/// Value computation is deferred until the first call to the <see cref="Get"/> method.
/// </summary>
/// <typeparam name="T">The type of the lazily initialized value.</typeparam>
/// <remarks>
/// <para>
/// Implementations guarantee that value computation occurs at most once,
/// even in multithreaded scenarios.
/// </para>
/// <para>
/// After the first computation, the value supplier is released for garbage collection.
/// </para>
/// </remarks>
public interface ILazy<T>
{
    /// <summary>
    /// Gets the lazily initialized value.
    /// </summary>
    /// <returns>
    /// The computed value of type <typeparamref name="T"/>. May be <c>null</c>
    /// if the value supplier returns <c>null</c>.
    /// </returns>
    /// <para>
    /// On first call: invokes the value supplier, caches the result, and returns it.
    /// </para>
    /// <para>
    /// On subsequent calls: returns the previously computed value without recomputation.
    /// </para>
    T? Get();
}
