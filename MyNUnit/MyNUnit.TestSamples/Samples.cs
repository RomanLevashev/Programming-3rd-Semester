using System;
using System.Threading;
using MyNUnit.Core;

namespace MyNUnit.TestSamples;

/// <summary>
/// A minimal class with a single passing test.
/// </summary>
public class SimplePassingTests
{
    /// <summary>
    /// A trivial test that always succeeds.
    /// </summary>
    [Test]
    public void ShouldPass()
    {
    }
}

public class IgnoredSampleTests
{
    [Test(Ignore = "Демонстрационный пропуск")]
    public void ShouldBeIgnored()
    {
    }
}

public class ExpectedExceptionTests
{
    [Test(Expected = typeof(InvalidOperationException))]
    public void ThrowsExpected()
    {
        throw new InvalidOperationException("Ожидаемое исключение");
    }
}

public class UnexpectedExceptionTests
{
    [Test(Expected = typeof(InvalidOperationException))]
    public void ThrowsDifferent()
    {
        throw new ArgumentException("Другое исключение");
    }

    [Test]
    public void ThrowsWithoutExpectation()
    {
        throw new InvalidOperationException("Неожиданное исключение");
    }
}

public class LifecycleSample
{
    private static int beforeClassCalls;
    private static int afterClassCalls;
    private static int beforeCalls;
    private static int afterCalls;

    public static void Reset()
    {
        beforeClassCalls = 0;
        afterClassCalls = 0;
        beforeCalls = 0;
        afterCalls = 0;
    }

    public static (int beforeClass, int afterClass, int before, int after) GetCounters() =>
        (beforeClassCalls, afterClassCalls, beforeCalls, afterCalls);

    [BeforeClass]
    public static void SetupClass() => Interlocked.Increment(ref beforeClassCalls);

    [AfterClass]
    public static void TearDownClass() => Interlocked.Increment(ref afterClassCalls);

    [Before]
    public void Setup() => Interlocked.Increment(ref beforeCalls);

    [After]
    public void TearDown() => Interlocked.Increment(ref afterCalls);

    [Test]
    public void Passing() { }
}

public class SlowSampleOne
{
    public static DateTime Start { get; private set; }
    public static DateTime End { get; private set; }

    public static void Reset()
    {
        Start = DateTime.MinValue;
        End = DateTime.MinValue;
    }

    [Test]
    public void SlowTest()
    {
        Start = DateTime.UtcNow;
        Thread.Sleep(350);
        End = DateTime.UtcNow;
    }
}

public class SlowSampleTwo
{
    public static DateTime Start { get; private set; }
    public static DateTime End { get; private set; }

    public static void Reset()
    {
        Start = DateTime.MinValue;
        End = DateTime.MinValue;
    }

    [Test]
    public void AnotherSlowTest()
    {
        Start = DateTime.UtcNow;
        Thread.Sleep(350);
        End = DateTime.UtcNow;
    }
}
