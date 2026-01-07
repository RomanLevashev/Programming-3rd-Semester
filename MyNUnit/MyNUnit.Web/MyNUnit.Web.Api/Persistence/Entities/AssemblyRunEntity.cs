// <copyright file="AssemblyRunEntity.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Web.Api.Persistence.Entities;

/// <summary>
/// Represents aggregated results for a single assembly within a run.
/// </summary>
public sealed class AssemblyRunEntity
{
    /// <summary>
    /// Gets or sets the database identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the associated test run identifier.
    /// </summary>
    public Guid TestRunId { get; set; }

    /// <summary>
    /// Gets or sets the parent test run entity.
    /// </summary>
    public TestRunEntity? TestRun { get; set; }

    /// <summary>
    /// Gets or sets the related upload identifier, if available.
    /// </summary>
    public Guid? AssemblyUploadId { get; set; }

    /// <summary>
    /// Gets or sets the assembly name.
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of passed tests for this assembly.
    /// </summary>
    public int Passed { get; set; }

    /// <summary>
    /// Gets or sets the number of failed tests for this assembly.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Gets or sets the number of ignored tests for this assembly.
    /// </summary>
    public int Ignored { get; set; }

    /// <summary>
    /// Gets or sets the total duration for this assembly in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the per-test results for this assembly.
    /// </summary>
    public List<TestResultEntity> Tests { get; set; } = new();
}
