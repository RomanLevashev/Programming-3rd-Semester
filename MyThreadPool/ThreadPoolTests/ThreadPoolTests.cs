// <copyright file="ThreadPoolTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace ThreadPoolTests;

using MyThreadPool;

/// <summary>
/// Contains unit tests for verifying the functionality and behavior of the <see cref="MyThreadPool"/> class.
/// </summary>
/// <remarks>
/// This test class validates various aspects of the thread pool implementation including:
/// - Basic task execution and result retrieval
/// - Thread pool initialization with specified thread counts
/// - Concurrent task execution across multiple threads
/// - Proper synchronization and thread safety mechanisms.
/// </remarks>
[TestClass]
public sealed class ThreadPoolTests
{
    /// <summary>
    /// Verifies that the thread pool can execute multiple basic tasks concurrently
    /// and return correct results for all tasks.
    /// </summary>
    /// <remarks>
    /// This test submits multiple simple computation tasks to the thread pool and
    /// validates that all tasks complete execution and return expected results.
    /// The test ensures basic task submission and result retrieval functionality.
    /// </remarks>
    [TestMethod]
    public void ExecuteMultipleBasicTasks_ReturnsCorrectResultsForAllTasks()
    {
        using var threadPool = new MyThreadPool(2);

        var results = new List<int>();
        var tasks = new List<IMyTask<int>>();

        for (int i = 0; i < 5; i++)
        {
            int capture = i;
            tasks.Add(threadPool.QueueUserWorkItem(() => capture * 2));
        }

        foreach (var task in tasks)
        {
            results.Add(task.Result);
        }

        CollectionAssert.AreEqual(results, new[] { 0, 2, 4, 6, 8 });
    }

    /// <summary>
    /// Verifies that the thread pool maintains the specified number of worker threads
    /// and can execute tasks concurrently across all threads.
    /// </summary>
    /// <remarks>
    /// This test validates that the thread pool correctly initializes with the requested
    /// number of threads and that all threads become active and process tasks simultaneously.
    /// The test uses synchronization primitives to ensure all threads are actively working
    /// before verifying the thread count.
    /// </remarks>
    [TestMethod]
    public void InitializeThreadPool_WithSpecifiedThreadCount_AllThreadsProcessTasksConcurrently()
    {
        int threads = 4;
        using var pool = new MyThreadPool(threads);
        var startEvent = new ManualResetEventSlim();
        var completionEvent = new ManualResetEventSlim();
        var countdown = new CountdownEvent(threads);
        var runningCount = 0;

        for (int i = 0; i < threads; i++)
        {
            pool.QueueUserWorkItem(() =>
            {
                startEvent.Wait();
                Interlocked.Increment(ref runningCount);
                countdown.Signal();
                completionEvent.Wait();

                return 0;
            });
        }

        startEvent.Set();
        countdown.Wait();
        Assert.AreEqual(threads, Volatile.Read(ref runningCount));
        completionEvent.Set();
    }
}
