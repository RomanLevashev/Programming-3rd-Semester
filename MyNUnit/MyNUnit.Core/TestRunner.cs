// <copyright file="TestRunner.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Discovers and executes MyNUnit test assemblies and aggregates their results.
/// </summary>
public sealed class TestRunner
{
    private readonly int maxDegreeOfParallelism;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestRunner"/> class.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Optional override for the maximum number of concurrent workers.</param>
    public TestRunner(int? maxDegreeOfParallelism = null)
    {
        this.maxDegreeOfParallelism = maxDegreeOfParallelism ?? Math.Max(1, Environment.ProcessorCount - 1);
    }

    /// <summary>
    /// Recursively finds managed assemblies under the given root path.
    /// </summary>
    /// <param name="rootPath">Root directory to scan.</param>
    /// <returns>Enumeration of assembly file paths.</returns>
    public static IEnumerable<string> DiscoverAssemblies(string rootPath)
    {
        return Directory.EnumerateFiles(rootPath, "*.dll", SearchOption.AllDirectories)
            .Where(IsManagedAssembly);
    }

    /// <summary>
    /// Discovers test assemblies in the given folder and runs all tests within them.
    /// </summary>
    /// <param name="rootPath">Path to the directory that contains compiled test assemblies.</param>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    /// <returns>Aggregated test run results.</returns>
    public async Task<TestRunResult> RunPathAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Path '{rootPath}' does not exists.");
        }

        var assemblies = DiscoverAssemblies(rootPath).ToArray();
        return await this.RunAssembliesAsync(assemblies, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs tests found in the specified assemblies.
    /// </summary>
    /// <param name="assemblyPaths">Paths to assemblies that should be executed.</param>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    /// <returns>Aggregated test run results.</returns>
    public async Task<TestRunResult> RunAssembliesAsync(IEnumerable<string> assemblyPaths, CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<TestResult>();
        var assemblyList = assemblyPaths.Where(IsManagedAssembly).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        await Parallel.ForEachAsync(
            assemblyList,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = this.maxDegreeOfParallelism,
            },
            (assemblyPath, _) =>
            {
                this.RunAssembly(assemblyPath, results);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

        return new TestRunResult(results.ToList());
    }

    private static (TestStatus Status, string? Details) InvokeTestMethod(MethodInfo testMethod, object? instance, Type? expectedException)
    {
        try
        {
            testMethod.Invoke(instance, Array.Empty<object?>());
            if (expectedException != null)
            {
                return (TestStatus.Failed, $"Expection {expectedException.FullName} was expected, but it was not thrown");
            }

            return (TestStatus.Passed, null);
        }
        catch (TargetInvocationException tex)
        {
            var actual = tex.InnerException ?? tex;
            return HandleTestException(expectedException, actual);
        }
        catch (Exception ex)
        {
            return HandleTestException(expectedException, ex);
        }
    }

    private static (TestStatus Status, string? Details) HandleTestException(Type? expectedException, Exception actualException)
    {
        if (expectedException == null)
        {
            return (TestStatus.Failed, actualException.ToString());
        }

        if (expectedException.IsAssignableFrom(actualException.GetType()))
        {
            return (TestStatus.Passed, null);
        }

        return (TestStatus.Failed,
            $"Expected exception {expectedException.FullName}, got {actualException.GetType().FullName}: {actualException}");
    }

    private static void InvokeLifecycle(IReadOnlyList<MethodInfo> methods, object? instance)
    {
        foreach (var method in methods)
        {
            method.Invoke(instance, Array.Empty<object?>());
        }
    }

    private static bool HasTests(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Any(m => m.HasAttribute<TestAttribute>());

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string FormatError(string phase, Exception ex) =>
        $"{phase} failure: {Unwrap(ex)}";

    private static Exception Unwrap(Exception ex) =>
        ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;

    private void RunAssembly(string assemblyPath, ConcurrentBag<TestResult> results)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception ex)
        {
            results.Add(new TestResult(
                Path.GetFileName(assemblyPath),
                "<assembly>",
                "Load",
                TestStatus.Failed,
                TimeSpan.Zero,
                ex.ToString()));
            return;
        }

        var assemblyName = assembly.GetName().Name ?? Path.GetFileName(assemblyPath);
        var testClasses = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && HasTests(t))
            .ToArray();

        Parallel.ForEach(
            testClasses,
            new ParallelOptions { MaxDegreeOfParallelism = this.maxDegreeOfParallelism },
            testClass => this.RunTestClass(assemblyName, testClass, results));
    }

    private void RunTestClass(string assemblyName, Type testClass, ConcurrentBag<TestResult> results)
    {
        var testMethods = testClass
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Select(method => (method, attribute: method.GetCustomAttribute<TestAttribute>()))
            .Where(pair => pair.attribute != null)
            .ToArray();

        if (testMethods.Length == 0)
        {
            return;
        }

        string className = testClass.FullName ?? testClass.Name;

        IReadOnlyList<MethodInfo> beforeClass;
        IReadOnlyList<MethodInfo> afterClass;
        IReadOnlyList<MethodInfo> before;
        IReadOnlyList<MethodInfo> after;

        beforeClass = this.GetLifecycleMethods<BeforeClassAttribute>(testClass, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        afterClass = this.GetLifecycleMethods<AfterClassAttribute>(testClass, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        before = this.GetLifecycleMethods<BeforeAttribute>(testClass, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        after = this.GetLifecycleMethods<AfterAttribute>(testClass, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        try
        {
            InvokeLifecycle(beforeClass, null);
        }
        catch (Exception ex)
        {
            var message = FormatError("BeforeClass", ex);
            foreach (var (method, _) in testMethods)
            {
                results.Add(new TestResult(
                    assemblyName,
                    className,
                    method.Name,
                    TestStatus.Failed,
                    TimeSpan.Zero,
                    message));
            }

            return;
        }

        foreach (var (method, attribute) in testMethods)
        {
            var testResult = this.RunSingleTest(assemblyName, className, testClass, method, attribute!, before, after);
            results.Add(testResult);
        }

        try
        {
            InvokeLifecycle(afterClass, null);
        }
        catch (Exception ex)
        {
            results.Add(new TestResult(
                assemblyName,
                className,
                "[AfterClass]",
                TestStatus.Failed,
                TimeSpan.Zero,
                FormatError("AfterClass", ex)));
        }
    }

    private TestResult RunSingleTest(
        string assemblyName,
        string className,
        Type testClass,
        MethodInfo testMethod,
        TestAttribute attribute,
        IReadOnlyList<MethodInfo> before,
        IReadOnlyList<MethodInfo> after)
    {
        if (attribute.Ignore is { Length: > 0 } ignoreMessage)
        {
            return new TestResult(
                assemblyName,
                className,
                testMethod.Name,
                TestStatus.Ignored,
                TimeSpan.Zero,
                ignoreMessage);
        }

        if (testMethod.GetParameters().Length > 0)
        {
            return new TestResult(
                assemblyName,
                className,
                testMethod.Name,
                TestStatus.Failed,
                TimeSpan.Zero,
                "Test methods must be parameterless.");
        }

        if (testMethod.ReturnType != typeof(void))
        {
            return new TestResult(
                assemblyName,
                className,
                testMethod.Name,
                TestStatus.Failed,
                TimeSpan.Zero,
                "Test methods must return void");
        }

        object? instance = null;
        var status = TestStatus.Passed;
        string? details = null;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var needsInstance = !testMethod.IsStatic || before.Count > 0 || after.Count > 0;
            if (needsInstance)
            {
                instance = Activator.CreateInstance(testClass)
                           ?? throw new InvalidOperationException($"Failed to create an instance of {className}");
            }

            InvokeLifecycle(before, instance);

            var (testStatus, testDetails) = InvokeTestMethod(testMethod, instance, attribute.Expected);
            status = testStatus;
            details = testDetails;
        }
        catch (Exception ex)
        {
            status = TestStatus.Failed;
            details = FormatError("Test", ex);
        }
        finally
        {
            try
            {
                InvokeLifecycle(after, instance);
            }
            catch (Exception ex)
            {
                var afterError = FormatError("After", ex);
                if (status == TestStatus.Passed)
                {
                    status = TestStatus.Failed;
                    details = afterError;
                }
                else
                {
                    details = string.Join(Environment.NewLine, details, afterError).Trim();
                }
            }
        }

        stopwatch.Stop();

        return new TestResult(
            assemblyName,
            className,
            testMethod.Name,
            status,
            stopwatch.Elapsed,
            details);
    }

    private IReadOnlyList<MethodInfo> GetLifecycleMethods<TAttribute>(
        Type testClass,
        BindingFlags flags)
        where TAttribute : Attribute
    {
        var methods = testClass
            .GetMethods(flags)
            .Where(m => m.HasAttribute<TAttribute>())
            .ToArray();

        return methods;
    }
}
