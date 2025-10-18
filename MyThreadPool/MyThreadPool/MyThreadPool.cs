// <copyright file="MyThreadPool.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyThreadPool;

using System.Collections.Concurrent;

/// <summary>
/// Provides a thread pool with a fixed number of threads for executing asynchronous operations.
/// Supports task continuations, exception propagation, and graceful shutdown.
/// </summary>
/// <remarks>
/// This thread pool maintains a fixed number of worker threads that process tasks from a shared queue.
/// Tasks are represented as <see cref="IMyTask{TResult}"/> objects and support continuation operations.
/// The pool can be gracefully shut down, allowing running tasks to complete while rejecting new submissions.
/// </remarks>
public class MyThreadPool : IDisposable
{
    private readonly BlockingCollection<Action> taskQueue = [];
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Thread[] threads;
    private volatile bool isShutdown = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MyThreadPool"/> class with the specified number of threads.
    /// </summary>
    /// <param name="threadCount">The number of worker threads in the pool. Must be a positive integer.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="threadCount"/> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// The worker threads are started immediately upon pool creation and will begin processing tasks
    /// as they are submitted to the queue.
    /// </remarks>
    public MyThreadPool(int threadCount)
    {
        if (threadCount <= 0)
        {
            throw new ArgumentException("Thread count must be positive", nameof(threadCount));
        }

        this.threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            this.threads[i] = new Thread(this.WorkerThread)
            {
                IsBackground = true,
            };

            this.threads[i].Start();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the thread pool has been shut down.
    /// </summary>
    /// <value>
    /// <c>true</c> if the thread pool is shutting down or has been shut down; otherwise, <c>false</c>.
    /// </value>
    public bool IsShutdown => this.isShutdown;

    /// <summary>
    /// Gets the synchronization object used for shutdown operations.
    /// </summary>
    /// <value>
    /// An object used to synchronize access during shutdown and queue procedures.
    /// </value>
    /// <remarks>
    /// This lock is used internally to ensure thread-safe shutdown operations.
    /// </remarks>
    internal object ShutdownLock { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the thread pool is waiting for pending tasks to complete during shutdown.
    /// </summary>
    /// <value>
    /// <c>true</c> if the pool is waiting for pending tasks to complete; otherwise, <c>false</c>.
    /// </value>
    internal bool IsWaitingForPendingTasks { get; private set; } = true;

    /// <summary>
    /// Queues a work item to the thread pool and returns a task representing the operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the function.</typeparam>
    /// <param name="function">The function to execute asynchronously.</param>
    /// <returns>
    /// A task that represents the queued work and provides access to the result.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="function"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the thread pool is shutting down and cannot accept new tasks.
    /// </exception>
    /// <remarks>
    /// The function will be executed by one of the pool's worker threads when it becomes available.
    /// The returned task can be used to await the result or attach continuations.
    /// </remarks>
    public IMyTask<TResult> QueueUserWorkItem<TResult>(Func<TResult> function)
    {
        lock (this.ShutdownLock)
        {
            if (this.isShutdown)
            {
                throw new InvalidOperationException("ThreadPool is shutting down.");
            }

            var task = new MyTask<TResult>(function, this);
            this.taskQueue.Add(() => task.Invoke());

            return task;
        }
    }

    /// <summary>
    /// Initiates shutdown of the thread pool.
    /// </summary>
    /// <param name="waitForPendingTasks">
    /// <c>true</c> to allow all queued and running tasks to complete;
    /// <c>false</c> to cancel pending tasks and allow only running tasks to complete.
    /// </param>
    /// <remarks>
    /// <para>
    /// When <paramref name="waitForPendingTasks"/> is <c>true</c>, the method will block until all
    /// currently running and queued tasks have completed execution.
    /// </para>
    /// <para>
    /// When <paramref name="waitForPendingTasks"/> is <c>false</c>, the method will cancel all
    /// pending tasks in the queue and block only until currently running tasks complete.
    /// </para>
    /// <para>
    /// After shutdown is initiated, no new tasks can be submitted to the pool.
    /// </para>
    /// </remarks>
    public void Shutdown(bool waitForPendingTasks = true)
    {
        lock (this.ShutdownLock)
        {
            if (this.isShutdown)
            {
                return;
            }

            this.isShutdown = true;

            if (!waitForPendingTasks)
            {
                this.cancellationTokenSource.Cancel();
                this.IsWaitingForPendingTasks = false;
            }

            this.taskQueue.CompleteAdding();
        }

        foreach (var thread in this.threads)
        {
            thread.Join();
        }

        this.cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="MyThreadPool"/>.
    /// </summary>
    /// <remarks>
    /// Calls <see cref="Shutdown(bool)"/> to gracefully shut down the thread pool.
    /// This method blocks until all worker threads have terminated.
    /// </remarks>
    public void Dispose()
    {
        this.Shutdown();
    }

    /// <summary>
    /// Queues a work item to the thread pool without returning a task.
    /// </summary>
    /// <param name="workItem">The action to execute asynchronously.</param>
    /// <remarks>
    /// This method is intended for internal use and tasks that don't produce a result.
    /// If the thread pool is shutting down, the work item will not be queued.
    /// </remarks>
    internal void QueueWorkItem(Action workItem)
    {
        if (!this.isShutdown)
        {
            this.taskQueue.Add(workItem);
        }
    }

    private void WorkerThread()
    {
        try
        {
            foreach (var task in this.taskQueue.GetConsumingEnumerable(this.cancellationTokenSource.Token))
            {
                task();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}