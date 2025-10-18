// <copyright file="ShutdownTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace ThreadPoolTests;

using System.Diagnostics;
using MyThreadPool;

/// <summary>
/// Contains unit tests for verifying the shutdown behavior and resource cleanup functionality
/// of the <see cref="MyThreadPool"/> class.
/// </summary>
/// <remarks>
/// This test class validates various shutdown scenarios including:
/// - Graceful shutdown with completion of running and queued tasks
/// - Immediate shutdown with cancellation of pending tasks
/// - Continuation task behavior during shutdown
/// - Thread pool resource cleanup and disposal
/// - Timeout and blocking behavior during shutdown operations.
/// </remarks>
[TestClass]
public class ShutdownTests
{
    /// <summary>
    /// Verifies that shutdown waits for all started tasks to complete and does not return control
    /// until all threads have stopped when waitForPendingTasks parameter is true.
    /// </summary>
    [TestMethod]
    public void Shutdown_WaitsForAllStartedTasksToCompleteAndDoesNotReturnControlUntillAllThreadsStopped_WhenWaitForPendingTasksIsTrue()
        => this.Shutdown_WaitsForAllStartedTasksToCompleteAndDoesNotReturnControlUntillAllThreadsStopped(true);

    /// <summary>
    /// Verifies that shutdown waits for all started tasks to complete and does not return control
    /// until all threads have stopped when waitForPendingTasks parameter is false.
    /// </summary>
    [TestMethod]
    public void Shutdown_WaitsForAllStartedTasksToCompleteAndDoesNotReturnControlUntillAllThreadsStopped_WhenWaitForPendingTasksIsFalse()
        => this.Shutdown_WaitsForAllStartedTasksToCompleteAndDoesNotReturnControlUntillAllThreadsStopped(false);

    /// <summary>
    /// Verifies that shutdown completes all queued tasks when waitForPendingTasks parameter is true.
    /// </summary>
    [TestMethod]
    public void Shutdown_CompletesAllQueuedTasks_WhenWaitForPendingTasksIsTrue()
    {
        var threads = 4;
        var tasksCount = threads * 2;
        var pool = new MyThreadPool(threads);
        var tasks = new IMyTask<int>[tasksCount];
        var blocker = new ManualResetEventSlim();
        var countdown = new CountdownEvent(threads);

        for (int i = 0; i < tasksCount; i++)
        {
            int capture = i;

            tasks[i] = pool.QueueUserWorkItem(() =>
            {
                if (capture < threads)
                {
                    countdown.Signal();
                }

                blocker.Wait();
                Thread.Sleep(200);

                return capture;
            });
        }

        countdown.Wait();
        blocker.Set();
        pool.Shutdown(true);

        for (int i = 0; i < tasksCount; i++)
        {
            Assert.AreEqual(i, tasks[i].Result);
        }
    }

    /// <summary>
    /// Verifies that shutdown does not execute pending queue tasks when waitForPendingTasks parameter is false.
    /// </summary>
    [TestMethod]
    public void Shutdown_DoesNotExecutePendingQueueTasks_WhenWaitForPendingTasksIsFalse()
    {
        var threads = 4;
        var tasksCount = threads * 2;
        var pool = new MyThreadPool(threads);
        var tasks = new IMyTask<int>[tasksCount];
        var blocker = new ManualResetEventSlim();
        var countdown = new CountdownEvent(threads);

        for (int i = 0; i < tasksCount; i++)
        {
            int capture = i;
            tasks[i] = pool.QueueUserWorkItem(() =>
            {
                if (capture < threads)
                {
                    countdown.Signal();
                }

                blocker.Wait();
                Thread.Sleep(200);

                return capture;
            });
        }

        countdown.Wait();
        blocker.Set();

        pool.Shutdown(false);

        for (int i = threads; i < tasksCount; i++)
        {
            int capture = i;
            Assert.ThrowsException<OperationCanceledException>(() => tasks[capture].Result);
        }
    }

    /// <summary>
    /// Verifies that shutdown completes continuations in queue when waitForPendingTasks parameter is true.
    /// </summary>
    [TestMethod]
    public void Shutdown_CompleteContinuationsInQueue_WhenWaitForPendingTasksIsTrue()
    {
        var threads = 4;
        var continuationsCount = threads;
        var pool = new MyThreadPool(threads);
        var continuationsBlocker = new ManualResetEventSlim();
        var countdown = new CountdownEvent(continuationsCount);

        var task = pool.QueueUserWorkItem(() =>
        {
            return 4;
        });

        var continuations = new IMyTask<int>[continuationsCount];

        for (int i = 0; i < continuationsCount; i++)
        {
            int capture = i;
            continuations[i] = pool.QueueUserWorkItem(() =>
            {
                countdown.Signal();
                continuationsBlocker.Wait();

                Thread.Sleep(200);
                return capture;
            });
        }

        countdown.Wait();
        continuationsBlocker.Set();
        pool.Shutdown(true);

        for (int i = 0; i < continuationsCount; i++)
        {
            Assert.AreEqual(i, continuations[i].Result);
        }
    }

    /// <summary>
    /// Verifies that shutdown does not execute continuations queue tasks when waitForPendingTasks parameter is false.
    /// </summary>
    [TestMethod]
    public void Shutdown_DoesNotExecuteContinuationsQueueTasks_WhenWaitForPendingTasksIsFalse()
    {
        var threads = 4;
        var continuationsCount = threads * 2;
        var pool = new MyThreadPool(threads);
        var continuationsBlocker = new ManualResetEventSlim();
        var countdown = new CountdownEvent(threads);

        var task = pool.QueueUserWorkItem(() =>
        {
            return 4;
        });

        var continuations = new IMyTask<int>[continuationsCount];

        for (int i = 0; i < continuationsCount; i++)
        {
            int capture = i;
            continuations[i] = pool.QueueUserWorkItem(() =>
            {
                if (capture < threads)
                {
                    countdown.Signal();
                }

                continuationsBlocker.Wait();

                Thread.Sleep(200);
                return capture;
            });
        }

        countdown.Wait();
        continuationsBlocker.Set();
        pool.Shutdown(false);

        for (int i = threads; i < continuationsCount; i++)
        {
            int capture = i;
            Assert.ThrowsException<OperationCanceledException>(() => continuations[i].Result);
        }
    }

    private void Shutdown_WaitsForAllStartedTasksToCompleteAndDoesNotReturnControlUntillAllThreadsStopped(bool waitForPendingTask)
    {
        int threads = 4;
        var pool = new MyThreadPool(threads);
        var tasks = new IMyTask<int>[threads];
        var blocker = new ManualResetEventSlim();
        var countdown = new CountdownEvent(threads);

        for (int i = 0; i < threads; i++)
        {
            int capture = i;
            tasks[i] = pool.QueueUserWorkItem(() =>
            {
                blocker.Wait();
                countdown.Signal();
                Thread.Sleep(200);
                return capture;
            });
        }

        blocker.Set();
        countdown.Wait();

        var shutdownStopwatch = Stopwatch.StartNew();
        pool.Shutdown(waitForPendingTask);
        shutdownStopwatch.Stop();
        Assert.IsTrue(shutdownStopwatch.ElapsedMilliseconds > 150);

        for (int i = 0; i < threads; i++)
        {
            Assert.AreEqual(i, tasks[i].Result);
        }
    }
}
