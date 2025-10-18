// <copyright file="ContinuationTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace ThreadPoolTests;

using System.Diagnostics;
using MyThreadPool;

/// <summary>
/// Contains unit tests for verifying task continuation functionality and behavior
/// in the <see cref="IMyTask{TResult}"/> implementation.
/// </summary>
/// <remarks>
/// This test class validates various continuation scenarios including:
/// - Basic continuation chain execution and result transformation
/// - Non-blocking behavior of ContinueWith method calls
/// - Proper execution ordering between parent tasks and their continuations
/// - Continuation scheduling and execution timing.
/// </remarks>
[TestClass]
public class ContinuationTests
{
    /// <summary>
    /// Verifies that basic continuation chains execute correctly and produce
    /// the expected transformed results through multiple continuation steps.
    /// </summary>
    [TestMethod]
    public void TestBasicContinuation()
    {
        using var pool = new MyThreadPool(2);

        var task = pool.QueueUserWorkItem(() => 10).ContinueWith(x => x * 2).ContinueWith(x => x.ToString());

        Assert.AreEqual("20", task.Result);
        Assert.IsTrue(task.IsCompleted);
    }

    /// <summary>
    /// Verifies that the ContinueWith method returns immediately without blocking
    /// and that continuations execute only after the parent task has completed.
    /// </summary>
    [TestMethod]
    public void ContinueWithIsNonBlockingAndExecutesOnlyAfterParentCompletion()
    {
        using var pool = new MyThreadPool(2);

        var parentStarted = new ManualResetEventSlim();
        var parentBlocked = new ManualResetEventSlim();
        var parentCompleted = new ManualResetEventSlim();
        var continuationExecuted = new ManualResetEventSlim();

        var task = pool.QueueUserWorkItem(() =>
        {
            parentStarted.Set();
            parentBlocked.Wait();
            parentCompleted.Set();
            return 42;
        });

        parentStarted.Wait();

        var stopwatch = Stopwatch.StartNew();

        var continuation = task.ContinueWith(x =>
        {
            continuationExecuted.Set();
            return x * 2;
        });

        stopwatch.Stop();

        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100);
        Assert.IsFalse(continuationExecuted.IsSet);

        Thread.Sleep(100);
        Assert.IsFalse(continuationExecuted.IsSet);

        parentBlocked.Set();

        Assert.IsTrue(parentCompleted.Wait(100));
        Assert.IsTrue(continuationExecuted.Wait(100));

        Assert.AreEqual(84, continuation.Result);
        Assert.IsTrue(continuation.IsCompleted);
    }
}
