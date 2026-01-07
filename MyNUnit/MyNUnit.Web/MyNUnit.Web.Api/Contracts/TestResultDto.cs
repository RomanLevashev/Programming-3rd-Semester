namespace MyNUnit.Web.Api.Contracts;

/// <summary>
/// Describes a single executed test.
/// </summary>
public sealed class TestResultDto
{
    /// <summary>
    /// Gets or sets the fully qualified test class name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test status as a string.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the failure or ignore details, when applicable.
    /// </summary>
    public string? Details { get; set; }
}
