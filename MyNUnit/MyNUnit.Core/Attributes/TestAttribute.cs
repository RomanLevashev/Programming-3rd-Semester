// <copyright file="TestAttribute.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

/// <summary>
/// Marks a test method and optionally specifies an expected exception or an ignore reason.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute
{
    /// <summary>
    /// Gets the exception type that should be thrown for the test to be considered successful.
    /// </summary>
    public Type? Expected { get; init; }

    /// <summary>
    /// Gets the message that explains why the test is skipped.
    /// </summary>
    public string? Ignore { get; init; }
}
