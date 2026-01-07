using System.Reflection;
using MyNUnit.Core;
using MyNUnit.TestSamples;
using NUnitTestAttribute = NUnit.Framework.TestAttribute;

namespace MyNUnit.Tests;

public class RunnerTests
{
    private static string SampleAssemblyPath =>
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "MyNUnit.TestSamples.dll");

    [NUnitTestAttribute]
    public async Task CountsPassedFailedAndIgnored()
    {
        var runner = new TestRunner(maxDegreeOfParallelism: 4);

        LifecycleSample.Reset();
        var result = await runner.RunAssembliesAsync(new[] { SampleAssemblyPath });

        Assert.That(result.Tests.Count, Is.GreaterThanOrEqualTo(5));
        Assert.That(result.Passed, Is.GreaterThanOrEqualTo(2));
        Assert.That(result.Failed, Is.GreaterThanOrEqualTo(2));
        Assert.That(result.Ignored, Is.GreaterThanOrEqualTo(1));

        var ignored = result.Tests.Single(t => t.MethodName == nameof(IgnoredSampleTests.ShouldBeIgnored));
        Assert.That(ignored.Status, Is.EqualTo(TestStatus.Ignored));
        Assert.That(ignored.Details, Does.Contain("Демонстрационный"));

        var expected = result.Tests.Single(t => t.MethodName == nameof(ExpectedExceptionTests.ThrowsExpected));
        Assert.That(expected.Status, Is.EqualTo(TestStatus.Passed));

        var unexpected = result.Tests.Single(t => t.MethodName == nameof(UnexpectedExceptionTests.ThrowsDifferent));
        Assert.That(unexpected.Status, Is.EqualTo(TestStatus.Failed));
        Assert.That(unexpected.Details, Does.Contain(nameof(InvalidOperationException)));
        Assert.That(unexpected.Details, Does.Contain(nameof(ArgumentException)));
    }

    [NUnitTestAttribute]
    public async Task LifecycleHooksAreExecuted()
    {
        var runner = new TestRunner();
        LifecycleSample.Reset();

        await runner.RunAssembliesAsync(new[] { SampleAssemblyPath });
        var counters = LifecycleSample.GetCounters();

        Assert.That(counters.beforeClass, Is.EqualTo(1));
        Assert.That(counters.afterClass, Is.EqualTo(1));
        Assert.That(counters.before, Is.EqualTo(1));
        Assert.That(counters.after, Is.EqualTo(1));
    }

    [NUnitTestAttribute]
    public async Task TestsFromDifferentClassesRunConcurrently()
    {
        var runner = new TestRunner(maxDegreeOfParallelism: 4);
        SlowSampleOne.Reset();
        SlowSampleTwo.Reset();

        await runner.RunAssembliesAsync(new[] { SampleAssemblyPath });

        Assert.That(SlowSampleOne.Start, Is.Not.EqualTo(DateTime.MinValue));
        Assert.That(SlowSampleTwo.Start, Is.Not.EqualTo(DateTime.MinValue));

        var overlap =
            SlowSampleOne.End > SlowSampleTwo.Start &&
            SlowSampleTwo.End > SlowSampleOne.Start;

        Assert.That(overlap, Is.True, "Медленные тесты должны перекрываться по времени (параллельный запуск разных классов).");
    }
}
