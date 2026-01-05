// <copyright file="TestRunResult.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

/// <summary>
/// Aggregates results of a full test run.
/// </summary>
/// <param name="Tests">All test results produced by the run.</param>
public sealed record TestRunResult(IReadOnlyList<TestResult> Tests)
{
    /// <summary>
    /// Gets the count of tests that passed.
    /// </summary>
    public int Passed => this.Tests.Count(t => t.Status == TestStatus.Passed);

    /// <summary>
    /// Gets the count of tests that failed.
    /// </summary>
    public int Failed => this.Tests.Count(t => t.Status == TestStatus.Failed);

    /// <summary>
    /// Gets the count of tests that were ignored.
    /// </summary>
    public int Ignored => this.Tests.Count(t => t.Status == TestStatus.Ignored);

    /// <summary>
    /// Gets the total execution time across all tests.
    /// </summary>
    public TimeSpan TotalDuration => TimeSpan.FromTicks(this.Tests.Sum(t => t.Duration.Ticks));
}
