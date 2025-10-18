// <copyright file="IMyTask{TResult}.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyThreadPool;

/// <summary>
/// Represents an asynchronous operation that returns a result of type <typeparamref name="TResult"/>.
/// Provides support for continuations and exception propagation.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by this task.</typeparam>
public interface IMyTask<TResult>
{
    /// <summary>
    /// Gets a value indicating whether gets a value that indicates whether the task has completed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the task has completed (either successfully, with cancellation, or with an exception);
    /// otherwise, <c>false</c>.
    /// </value>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets the result value of this <see cref="IMyTask{TResult}"/>.
    /// </summary>
    /// <value>
    /// The result value of this task, of type <typeparamref name="TResult"/>.
    /// </value>
    /// <exception cref="System.AggregateException">
    /// The task was canceled or threw an exception during execution.
    /// The exception(s) that caused the task to fail are contained in the <see cref="AggregateException.InnerExceptions"/> collection.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// The task was canceled before it could complete execution.
    /// </exception>
    TResult Result { get; }

    /// <summary>
    /// Creates a continuation that executes when the target task completes.
    /// </summary>
    /// <typeparam name="TNewResult">The type of the result produced by the continuation.</typeparam>
    /// <param name="continuation">A function to run when the task completes. When run, the delegate will be passed
    /// the completed task as an argument.</param>
    /// <returns>A new task that represents the continuation operation.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// The <paramref name="continuation"/> argument is <c>null</c>.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The thread pool is shutting down and cannot accept new tasks.
    /// </exception>
    /// <remarks>
    /// The returned task will not be scheduled for execution until the current task has completed, whether it completes
    /// due to running to completion successfully, faulting due to an unhandled exception, or exiting early due to being canceled.
    /// The continuation will be executed by a thread from the thread pool.
    /// </remarks>
    IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuation);
}