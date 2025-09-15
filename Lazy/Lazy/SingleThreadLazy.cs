// <copyright file="SingleThreadLazy.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lazy;

/// <summary>
/// Provides a simple, non-thread-safe implementation of lazy initialization.
/// Designed for single-threaded scenarios only.
/// </summary>
/// <typeparam name="T">The type of the lazily initialized value.</typeparam>
/// <para>
/// This implementation is optimized for performance in single-threaded environments
/// but should not be used in multithreaded scenarios as it does not provide
/// any synchronization mechanisms.
/// </para>
public class SingleThreadLazy<T> : ILazy<T>
{
    private Func<T?>? supplier;
    private T? value;
    private bool isValueCreated = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleThreadLazy{T}"/> class.
    /// </summary>
    /// <param name="supplier">The value factory delegate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="supplier"/> is <c>null</c>.
    /// </exception>
    public SingleThreadLazy(Func<T?> supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        this.supplier = supplier;
    }

    /// <summary>
    /// Gets the lazily initialized value.
    /// </summary>
    /// <returns>
    /// The initialized value. Returns <c>null</c> if the value factory returns <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// On the first call: invokes the value factory, caches the result, and returns it.
    /// </para>
    /// <para>
    /// On subsequent calls: returns the previously cached value without recomputation.
    /// </para>
    /// </remarks>
    public T? Get()
    {
        if (!this.isValueCreated)
        {
            this.value = this.supplier!();
            this.supplier = null;
            this.isValueCreated = true;
        }

        return this.value;
    }
}
