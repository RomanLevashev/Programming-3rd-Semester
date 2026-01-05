// <copyright file="TestStatus.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

/// <summary>
/// Represents the outcome of a single test execution.
/// </summary>
public enum TestStatus
{
    /// <summary>
    /// The test finished successfully.
    /// </summary>
    Passed,

    /// <summary>
    /// The test failed with an error or assertion.
    /// </summary>
    Failed,

    /// <summary>
    /// The test was skipped.
    /// </summary>
    Ignored,
}
