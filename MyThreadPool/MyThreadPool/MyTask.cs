// <copyright file="MyTask.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyThreadPool;

/// <summary>
/// Represents an asynchronous operation that produces a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by this task.</typeparam>
/// <remarks>
/// This class implements <see cref="IMyTask{TResult}"/> and provides the core functionality
/// for task execution, continuation chaining, and exception propagation within the thread pool.
/// </remarks>
internal class MyTask<TResult>(Func<TResult> function, MyThreadPool threadPool) : IMyTask<TResult>
{
    private readonly Func<TResult> function = function;
    private readonly MyThreadPool threadPool = threadPool;
    private readonly ManualResetEventSlim manualResetEvent = new(false);
    private readonly List<Action> continuations = [];
    private readonly object lockObject = new();
    private TResult? result;
    private AggregateException? exception;
    private volatile bool isCompleted;
    private volatile bool isBegin;

    /// <summary>
    /// Gets a value indicating whether the task has completed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the task has completed (successfully, with cancellation, or with an exception);
    /// otherwise, <c>false</c>.
    /// </value>
    public bool IsCompleted => this.isCompleted;

    /// <summary>
    /// Gets the result value of this task.
    /// </summary>
    /// <value>
    /// The result value of this task, of type <typeparamref name="TResult"/>.
    /// </value>
    /// <exception cref="System.AggregateException">
    /// The task was canceled or threw an exception during execution.
    /// The exception(s) that caused the task to fail are contained in the
    /// <see cref="AggregateException.InnerExceptions"/> collection.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// The task was canceled before it could complete execution.
    /// </exception>
    /// <remarks>
    /// This property blocks the calling thread until the task completes execution.
    /// If the task is canceled before starting execution, an <see cref="OperationCanceledException"/>
    /// is thrown. If the task fails during execution, an <see cref="AggregateException"/> is thrown
    /// containing all exceptions that occurred during execution.
    /// </remarks>
    public TResult Result
    {
        get
        {
            int waitTime = 100;
            while (!this.manualResetEvent.Wait(waitTime))
            {
                if (!this.threadPool.IsWaitingForPendingTasks && !this.isBegin)
                {
                    throw new OperationCanceledException();
                }

                waitTime = Math.Min(5000, waitTime * 2);
            }

            if (this.exception != null)
            {
                throw this.exception;
            }

            return this.result!;
        }
    }

    /// <summary>
    /// Creates a continuation that executes when the target task completes.
    /// </summary>
    /// <typeparam name="TNewResult">The type of the result produced by the continuation.</typeparam>
    /// <param name="continuation">A function to run when the task completes.</param>
    /// <returns>A new task that represents the continuation operation.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the thread pool is not associated with the task or when the thread pool is shutting down.
    /// </exception>
    /// <remarks>
    /// The continuation task will not be scheduled for execution until the current task has completed.
    /// If the current task has already completed, the continuation will be scheduled immediately.
    /// Otherwise, it will be added to the continuations list and executed after the current task completes.
    /// </remarks>
    public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuation)
    {
        if (this.threadPool is null)
        {
            throw new InvalidOperationException(nameof(this.threadPool));
        }

        lock (this.threadPool.ShutdownLock)
        {
            if (this.threadPool.IsShutdown)
            {
                throw new InvalidOperationException("Thread pool is shutting down");
            }

            var continuationTask = new MyTask<TNewResult>(() => continuation(this.Result), this.threadPool);

            lock (this.lockObject)
            {
                if (this.isCompleted)
                {
                    this.threadPool.QueueWorkItem(() => continuationTask.Invoke());
                }
                else
                {
                    this.continuations.Add(() => continuationTask.Invoke());
                }
            }

            return continuationTask;
        }
    }

    /// <summary>
    /// Executes the task function and handles completion logic.
    /// </summary>
    /// <remarks>
    /// This method is called by the thread pool worker threads to execute the task's function.
    /// It handles exception capturing, completion signaling, and continuation scheduling.
    /// If the thread pool is shutting down and not waiting for pending tasks, the method
    /// may exit early without executing the function.
    /// </remarks>
    internal void Invoke()
    {
        try
        {
            lock (this.threadPool.ShutdownLock)
            {
                if (this.threadPool.IsShutdown && !this.threadPool.IsWaitingForPendingTasks)
                {
                    this.manualResetEvent.Set();
                    return;
                }

                this.isBegin = true;
            }

            this.result = this.function();
        }
        catch (Exception ex)
        {
            this.exception = ex as AggregateException ?? new AggregateException(ex);
        }

        lock (this.lockObject)
        {
            this.isCompleted = true;
            this.manualResetEvent.Set();
        }

        if (this.continuations.Count == 0)
        {
            return;
        }

        lock (this.threadPool.ShutdownLock)
        {
            if (!this.threadPool.IsWaitingForPendingTasks)
            {
                return;
            }

            foreach (var continuation in this.continuations)
            {
                this.threadPool.QueueWorkItem(continuation);
            }
        }

        this.continuations.Clear();
    }
}
