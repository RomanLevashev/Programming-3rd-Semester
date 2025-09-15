// <copyright file="SingleThreadLazyTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace LazyTests;

using Lazy;

/// <summary>
/// Contains unit tests for the <see cref="SingleThreadLazy{T}"/> class.
/// Verifies correct behavior in single-threaded scenarios.
/// This implementation is not thread-safe and should only be used in single-threaded environments.
/// </summary>
[TestClass]
public class SingleThreadLazyTests : LazyTestsBase
{
    /// <summary>
    /// Creates a new instance of <see cref="SingleThreadLazy{T}"/> for testing.
    /// </summary>
    /// <typeparam name="T">The type of the lazily initialized value.</typeparam>
    /// <param name="supplier">The value factory delegate.</param>
    /// <returns>A new instance of <see cref="SingleThreadLazy{T}"/>.</returns>
    protected override ILazy<T> CreateLazy<T>(Func<T> supplier) => new SingleThreadLazy<T>(supplier!);
}
