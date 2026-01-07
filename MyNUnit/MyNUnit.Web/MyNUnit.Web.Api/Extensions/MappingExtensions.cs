using MyNUnit.Web.Api.Contracts;
using MyNUnit.Web.Api.Persistence.Entities;

namespace MyNUnit.Web.Api.Extensions;

/// <summary>
/// Maps persistence entities to API DTOs.
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Converts <see cref="UploadedAssembly"/> to <see cref="AssemblyUploadDto"/>.
    /// </summary>
    /// <param name="entity">Source entity.</param>
    /// <returns>Mapped DTO.</returns>
    public static AssemblyUploadDto ToDto(this UploadedAssembly entity) =>
        new()
        {
            Id = entity.Id,
            FileName = entity.OriginalFileName,
            SizeBytes = entity.SizeBytes,
            UploadedAt = entity.UploadedAt,
        };

    /// <summary>
    /// Converts <see cref="TestRunEntity"/> to a summary DTO.
    /// </summary>
    /// <param name="entity">Source entity.</param>
    /// <returns>Mapped summary DTO.</returns>
    public static TestRunSummaryDto ToSummaryDto(this TestRunEntity entity) =>
        new()
        {
            Id = entity.Id,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            Passed = entity.Passed,
            Failed = entity.Failed,
            Ignored = entity.Ignored,
            TotalDurationMs = entity.TotalDurationMs,
        };

    /// <summary>
    /// Converts <see cref="TestRunEntity"/> to a detailed DTO.
    /// </summary>
    /// <param name="entity">Source entity.</param>
    /// <returns>Mapped detailed DTO.</returns>
    public static TestRunDetailsDto ToDetailsDto(this TestRunEntity entity) =>
        new()
        {
            Id = entity.Id,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            Passed = entity.Passed,
            Failed = entity.Failed,
            Ignored = entity.Ignored,
            TotalDurationMs = entity.TotalDurationMs,
            Assemblies = entity.Assemblies
                .OrderBy(a => a.AssemblyName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.Id)
                .Select(a => new AssemblyRunDto
                {
                    Id = a.Id,
                    AssemblyUploadId = a.AssemblyUploadId,
                    AssemblyName = a.AssemblyName,
                    Passed = a.Passed,
                    Failed = a.Failed,
                    Ignored = a.Ignored,
                    DurationMs = a.DurationMs,
                    Tests = a.Tests
                        .OrderBy(t => t.ClassName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(t => t.MethodName, StringComparer.OrdinalIgnoreCase)
                        .Select(t => new TestResultDto
                        {
                            ClassName = t.ClassName,
                            MethodName = t.MethodName,
                            Status = t.Status.ToString(),
                            DurationMs = t.DurationMs,
                            Details = t.Details,
                        })
                        .ToList(),
                })
                .ToList(),
        };
}
