// <copyright file="MultiThreadLazyTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace LazyTests;

using System;
using System.Collections.Concurrent;
using Lazy;

/// <summary>
/// Contains unit tests for the <see cref="MultiThreadLazy{T}"/> class.
/// Verifies thread safety and correct behavior in multithreaded scenarios.
/// </summary>
[TestClass]
public class MultiThreadLazyTests : LazyTestsBase
{
    /// <summary>
    /// Verifies that the value is computed exactly once when accessed concurrently
    /// from multiple threads, and that all threads receive the same computed value.
    /// </summary>
    [TestMethod]
    public void GetShouldBeThreadSafe()
    {
        int callCount = 0;
        var lazy = this.CreateLazy(() =>
        {
            Interlocked.Increment(ref callCount);
            return 999;
        });

        var results = new ConcurrentBag<int>();
        var threads = new List<Thread>();
        int threadCount = 100;
        var startSignal = new ManualResetEventSlim(false);

        for (int i = 0; i < threadCount; i++)
        {
            var thread = new Thread(() =>
            {
                startSignal.Wait();
                results.Add(lazy.Get());
            });
            threads.Add(thread);
            thread.Start();
        }

        startSignal.Set();

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Assert.AreEqual(1, callCount, "Supplier should be called only once");
        Assert.AreEqual(threadCount, results.Count);
        Assert.IsTrue(results.All(x => x == 999), "All results should be equal");
    }

    /// <summary>
    /// Creates a new instance of <see cref="MultiThreadLazy{T}"/> for testing.
    /// </summary>
    /// <typeparam name="T">The type of the lazily initialized value.</typeparam>
    /// <param name="supplier">The value factory delegate.</param>
    /// <returns>A new instance of <see cref="MultiThreadLazy{T}"/>.</returns>
    protected override ILazy<T> CreateLazy<T>(Func<T> supplier) => new MultiThreadLazy<T>(supplier);
}
