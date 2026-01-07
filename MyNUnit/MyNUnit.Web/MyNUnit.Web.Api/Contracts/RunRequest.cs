// <copyright file="RunRequest.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Web.Api.Contracts;

/// <summary>
/// Describes a request to execute tests for selected assemblies.
/// </summary>
public sealed class RunRequest
{
    /// <summary>
    /// Gets or sets the assembly upload identifiers to execute.
    /// When omitted, all available assemblies are executed.
    /// </summary>
    public List<Guid>? AssemblyIds { get; set; }
}
