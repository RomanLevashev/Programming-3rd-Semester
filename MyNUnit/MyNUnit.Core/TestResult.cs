// <copyright file="TestResult.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

/// <summary>
/// Contains details about an executed test method.
/// </summary>
/// <param name="AssemblyName">Name of the assembly that hosted the test.</param>
/// <param name="ClassName">Fully qualified name of the test class.</param>
/// <param name="MethodName">Name of the test method.</param>
/// <param name="Status">Execution outcome.</param>
/// <param name="Duration">Elapsed time spent running the test.</param>
/// <param name="Details">Optional failure or ignore message.</param>
public sealed record TestResult(
    string AssemblyName,
    string ClassName,
    string MethodName,
    TestStatus Status,
    TimeSpan Duration,
    string? Details);
