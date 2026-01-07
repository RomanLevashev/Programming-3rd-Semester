namespace MyNUnit.Web.Api.Contracts;

/// <summary>
/// Represents a stored assembly upload shown to API clients.
/// </summary>
public sealed class AssemblyUploadDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the upload.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the original file name provided by the client.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the uploaded file in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the UTC time when the upload was stored.
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; }
}
