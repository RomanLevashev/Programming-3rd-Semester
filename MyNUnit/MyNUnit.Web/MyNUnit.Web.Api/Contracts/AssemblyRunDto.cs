namespace MyNUnit.Web.Api.Contracts;

/// <summary>
/// Describes aggregated results for a single test assembly.
/// </summary>
public sealed class AssemblyRunDto
{
    /// <summary>
    /// Gets or sets the identifier of this assembly run.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the upload identifier when the assembly was uploaded.
    /// </summary>
    public Guid? AssemblyUploadId { get; set; }

    /// <summary>
    /// Gets or sets the assembly name.
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of passed tests in the assembly.
    /// </summary>
    public int Passed { get; set; }

    /// <summary>
    /// Gets or sets the number of failed tests in the assembly.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Gets or sets the number of ignored tests in the assembly.
    /// </summary>
    public int Ignored { get; set; }

    /// <summary>
    /// Gets or sets the total duration for the assembly in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the detailed test results for the assembly.
    /// </summary>
    public List<TestResultDto> Tests { get; set; } = new();
}
