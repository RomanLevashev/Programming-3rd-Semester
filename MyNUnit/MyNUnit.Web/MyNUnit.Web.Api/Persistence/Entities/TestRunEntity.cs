namespace MyNUnit.Web.Api.Persistence.Entities;

/// <summary>
/// Represents a single test run with aggregated results.
/// </summary>
public sealed class TestRunEntity
{
    /// <summary>
    /// Gets or sets the run identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the UTC start time of the run.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC completion time of the run.
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of passed tests.
    /// </summary>
    public int Passed { get; set; }

    /// <summary>
    /// Gets or sets the number of failed tests.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Gets or sets the number of ignored tests.
    /// </summary>
    public int Ignored { get; set; }

    /// <summary>
    /// Gets or sets the total run duration in milliseconds.
    /// </summary>
    public double TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets per-assembly results for this run.
    /// </summary>
    public List<AssemblyRunEntity> Assemblies { get; set; } = new();
}
