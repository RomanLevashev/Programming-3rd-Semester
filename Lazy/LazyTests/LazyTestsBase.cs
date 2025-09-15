// <copyright file="LazyTestsBase.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace LazyTests;

using Lazy;

/// <summary>
/// Abstract base class containing common unit tests for all <see cref="ILazy{T}"/> implementations.
/// Provides shared test functionality for both single-threaded and multi-threaded lazy implementations.
/// </summary>
/// <remarks>
/// <para>
/// This base class defines the common contract that all lazy implementations must satisfy.
/// Derived test classes should implement the <see cref="CreateLazy{T}"/> factory method to
/// provide specific lazy instances for testing.
/// </para>
/// <para>
/// The tests in this class verify the core lazy initialization behavior that should be
/// consistent across all implementations, regardless of their threading model.
/// </para>
/// </remarks>
[TestClass]
public abstract class LazyTestsBase
{
    /// <summary>
    /// Verifies that the Get method returns the correct value computed by the value factory.
    /// </summary>
    /// <remarks>
    /// This test ensures that the lazy implementation correctly computes and returns
    /// the value produced by the supplied factory delegate.
    /// </remarks>
    [TestMethod]
    public void GetReturnCorrectValue()
    {
        var lazy = this.CreateLazy(() => 42);
        var result = lazy.Get();

        Assert.AreEqual(42, result);
    }

    /// <summary>
    /// Verifies that the value factory is invoked exactly once, regardless of how many times
    /// the Get method is called.
    /// </summary>
    /// <remarks>
    /// This test validates the fundamental lazy initialization behavior: the value is computed
    /// on first access and cached for subsequent accesses.
    /// </remarks>
    [TestMethod]
    public void GetShouldComputeValueOnlyOnce()
    {
        int callCount = 0;
        var lazy = this.CreateLazy(() =>
        {
            callCount++;
            return 100;
        });

        for (int i = 0; i < 10; i++)
        {
            lazy.Get();
        }

        Assert.AreEqual(1, callCount);
    }

    /// <summary>
    /// Verifies that the constructor throws an <see cref="ArgumentNullException"/>
    /// when a null supplier delegate is provided.
    /// </summary>
    [TestMethod]
    public void ConstructorThrowArgumentNullExceptionWhenSupplierIsNull()
    {
        Assert.ThrowsException<ArgumentNullException>(() => this.CreateLazy((Func<int>)null!));
    }

    /// <summary>
    /// Verifies that the lazy implementation correctly handles null values returned
    /// by the value factory.
    /// </summary>
    /// <remarks>
    /// The implementation should preserve the null value and not treat it as an error condition.
    /// </remarks>
    [TestMethod]
    public void GetShouldHandleNullValues()
    {
        var lazy = this.CreateLazy<string>(() => null!);
        var result = lazy.Get();

        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that multiple calls to Get return the same object instance for reference types,
    /// ensuring that the value is properly cached.
    /// </summary>
    /// <remarks>
    /// For reference types, this test ensures that the same object instance is returned
    /// on all subsequent calls, demonstrating proper caching behavior.
    /// </remarks>
    [TestMethod]
    public void GetShouldReturnSameObjectOnSubsequentCalls()
    {
        var lazy = this.CreateLazy(() => new object());
        var first = lazy.Get();
        var second = lazy.Get();
        var third = lazy.Get();

        Assert.AreSame(first, second);
        Assert.AreSame(second, third);
    }

    /// <summary>
    /// Factory method to create a new instance of the lazy implementation under test.
    /// </summary>
    /// <typeparam name="T">The type of the lazily initialized value.</typeparam>
    /// <param name="supplier">The value factory delegate.</param>
    /// <returns>A new instance of the lazy implementation.</returns>
    protected abstract ILazy<T> CreateLazy<T>(Func<T> supplier);
}
