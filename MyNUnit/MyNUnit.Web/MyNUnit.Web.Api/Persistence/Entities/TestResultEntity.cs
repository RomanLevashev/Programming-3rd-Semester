// <copyright file="TestResultEntity.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Web.Api.Persistence.Entities;

using MyNUnit.Core;

/// <summary>
/// Represents a stored test result.
/// </summary>
public sealed class TestResultEntity
{
    /// <summary>
    /// Gets or sets the database identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the parent assembly run identifier.
    /// </summary>
    public int AssemblyRunId { get; set; }

    /// <summary>
    /// Gets or sets the parent assembly run entity.
    /// </summary>
    public AssemblyRunEntity? AssemblyRun { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified test class name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test status.
    /// </summary>
    public TestStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the failure or ignore details, when applicable.
    /// </summary>
    public string? Details { get; set; }
}
