// <copyright file="ExceptionsTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace ThreadPoolTests;

using MyThreadPool;

/// <summary>
/// Contains unit tests for verifying exception handling and propagation behavior
/// in the <see cref="MyThreadPool"/> and <see cref="IMyTask{TResult}"/> implementations.
/// </summary>
/// <remarks>
/// This test class validates various exception scenarios including:
/// - Exception propagation from task functions to task results
/// - Exception handling in continuation chains
/// - AggregateException wrapping and inner exception preservation
/// - Exception behavior during task execution and result retrieval.
/// </remarks>
[TestClass]
public class ExceptionsTests
{
    /// <summary>
    /// Verifies that accessing the Result property of a task throws an AggregateException
    /// when the task function throws an exception during execution.
    /// </summary>
    [TestMethod]
    public void TaskResultThrowsAggregateExceptionWhenTaskFunctionThrowsException()
    {
        using var pool = new MyThreadPool(1);

        var task = pool.QueueUserWorkItem<int>(() => throw new DivideByZeroException());

        var exception = Assert.ThrowsException<AggregateException>(() => task.Result);

        Assert.IsInstanceOfType(exception.InnerException, typeof(DivideByZeroException));
    }

    /// <summary>
    /// Verifies that accessing the Result property of a continuation task throws an AggregateException
    /// when the continuation function throws an exception during execution.
    /// </summary>
    [TestMethod]
    public void TaskResultThrowsAggregateExceptionWhenContinuationFunctionThrowsException()
    {
        using var pool = new MyThreadPool(2);

        var task = pool.QueueUserWorkItem(() => 10).ContinueWith<int>(x => throw new DivideByZeroException());

        var exception = Assert.ThrowsException<AggregateException>(() => task.Result);
        Assert.IsInstanceOfType(exception.InnerException, typeof(DivideByZeroException));
    }

    /// <summary>
    /// Verifies that the original exception is propagated through multiple continuations
    /// when the initial task function throws an exception.
    /// </summary>
    [TestMethod]
    public void TaskResultPropagatesOriginalExceptionThroughMultipleContinuations()
    {
        using var pool = new MyThreadPool(2);

        var task = pool.QueueUserWorkItem<int>(() => throw new ArgumentException("Invalid argument"))
                      .ContinueWith(x => x + 1)
                      .ContinueWith(x => x * 2);

        var exception = Assert.ThrowsException<AggregateException>(() => task.Result);
        Assert.IsInstanceOfType(exception.InnerException, typeof(ArgumentException));
    }
}
