// <copyright file="AssemblyStorage.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Web.Api.Services;

using MyNUnit.Web.Api.Persistence.Entities;

/// <summary>
/// Stores uploaded assemblies on disk and provides metadata.
/// </summary>
public sealed class AssemblyStorage
{
    private readonly string rootPath;
    private readonly ILogger<AssemblyStorage> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyStorage"/> class.
    /// </summary>
    /// <param name="environment">Host environment for resolving storage paths.</param>
    /// <param name="logger">Logger instance.</param>
    public AssemblyStorage(IHostEnvironment environment, ILogger<AssemblyStorage> logger)
    {
        this.rootPath = Path.Combine(environment.ContentRootPath, "Storage", "Assemblies");
        Directory.CreateDirectory(this.rootPath);
        this.logger = logger;
    }

    /// <summary>
    /// Gets the root storage path for uploaded assemblies.
    /// </summary>
    public string RootPath => this.rootPath;

    /// <summary>
    /// Saves the uploaded file to disk and returns persisted metadata.
    /// </summary>
    /// <param name="file">Uploaded file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Persisted upload metadata.</returns>
    public async Task<UploadedAssembly> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var storedFileName = $"{id:N}-{SanitizeFileName(file.FileName)}";
        var destinationPath = Path.Combine(this.rootPath, storedFileName);

        await using (var stream = File.Create(destinationPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        this.logger.LogInformation("Stored assembly {Original} -> {Path}", file.FileName, destinationPath);

        return new UploadedAssembly
        {
            Id = id,
            OriginalFileName = file.FileName,
            StoredFileName = storedFileName,
            StoredPath = destinationPath,
            UploadedAt = DateTimeOffset.UtcNow,
            SizeBytes = file.Length,
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
