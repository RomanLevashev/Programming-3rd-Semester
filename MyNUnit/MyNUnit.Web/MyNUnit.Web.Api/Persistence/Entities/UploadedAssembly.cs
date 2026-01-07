// <copyright file="UploadedAssembly.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Web.Api.Persistence.Entities;

/// <summary>
/// Represents a file upload stored on disk.
/// </summary>
public sealed class UploadedAssembly
{
    /// <summary>
    /// Gets or sets the upload identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the original file name provided by the client.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stored file name on disk.
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute path of the stored file.
    /// </summary>
    public string StoredPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time when the upload was stored.
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the upload size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }
}
