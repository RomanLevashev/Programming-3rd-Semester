// <copyright file="MultiThreadLazy.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lazy;

/// <summary>
/// Thread-safe implementation of lazy initialization using double-checked locking pattern.
/// </summary>
/// <typeparam name="T">The type of the lazily initialized value.</typeparam>
public class MultiThreadLazy<T> : ILazy<T>
{
    private readonly object lockObject = new();
    private Func<T?>? supplier;
    private T? value;
    private bool isValueCreated = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiThreadLazy{T}"/> class.
    /// </summary>
    /// <param name="supplier">The value factory delegate.</param>
    /// <exception cref="ArgumentNullException">Thrown when supplier is null.</exception>
    public MultiThreadLazy(Func<T?> supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        this.supplier = supplier;
    }

    /// <summary>
    /// Gets the lazily initialized value.
    /// </summary>
    /// <returns>The initialized value.</returns>
    public T? Get()
    {
        if (!Volatile.Read(ref this.isValueCreated))
        {
            lock (this.lockObject)
            {
                if (!Volatile.Read(ref this.isValueCreated))
                {
                    this.value = this.supplier!();
                    this.supplier = null;
                    Volatile.Write(ref this.isValueCreated, true);
                }
            }
        }

        return this.value;
    }
}
