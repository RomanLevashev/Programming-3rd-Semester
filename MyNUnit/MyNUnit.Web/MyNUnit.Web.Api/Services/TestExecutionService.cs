// <copyright file="TestExecutionService.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Web.Api.Services;

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MyNUnit.Core;
using MyNUnit.Web.Api.Persistence;
using MyNUnit.Web.Api.Persistence.Entities;

/// <summary>
/// Executes tests using <see cref="TestRunner"/> and persists results.
/// </summary>
public sealed class TestExecutionService
{
    private readonly AppDbContext dbContext;
    private readonly TestRunner runner;
    private readonly ILogger<TestExecutionService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestExecutionService"/> class.
    /// </summary>
    /// <param name="dbContext">Database context used to persist results.</param>
    /// <param name="logger">Logger instance.</param>
    public TestExecutionService(AppDbContext dbContext, ILogger<TestExecutionService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.runner = new TestRunner();
    }

    /// <summary>
    /// Runs tests for the specified assembly uploads and stores results.
    /// </summary>
    /// <param name="assemblyIds">Assembly upload identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted test run entity.</returns>
    public async Task<TestRunEntity> RunAsync(IEnumerable<Guid> assemblyIds, CancellationToken cancellationToken)
    {
        var assemblies = await this.dbContext.UploadedAssemblies
            .Where(x => assemblyIds.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (assemblies.Count == 0)
        {
            throw new InvalidOperationException("Нет сборок для запуска. Загрузите хотя бы одну сборку.");
        }

        var stopwatch = Stopwatch.StartNew();
        var runId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow;

        TestRunResult runResult;
        try
        {
            var paths = assemblies.Select(x => x.StoredPath).ToArray();
            runResult = await this.runner.RunAssembliesAsync(paths, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to execute tests for run {RunId}", runId);
            throw;
        }

        stopwatch.Stop();
        var groupedByAssembly = runResult.Tests
            .GroupBy(t => t.AssemblyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var assemblyEntities = new List<AssemblyRunEntity>();
        foreach (var kvp in groupedByAssembly)
        {
            var assemblyName = kvp.Key;
            var tests = kvp.Value;
            var durationMs = tests.Sum(t => t.Duration.TotalMilliseconds);
            var (passed, failed, ignored) = Summarize(tests);

            var upload = assemblies.FirstOrDefault(a =>
                string.Equals(Path.GetFileNameWithoutExtension(a.StoredFileName), assemblyName, StringComparison.OrdinalIgnoreCase))
                ?? assemblies.FirstOrDefault(a =>
                    string.Equals(Path.GetFileNameWithoutExtension(a.OriginalFileName), assemblyName, StringComparison.OrdinalIgnoreCase));

            var assemblyEntity = new AssemblyRunEntity
            {
                AssemblyName = assemblyName,
                AssemblyUploadId = upload?.Id,
                DurationMs = durationMs,
                Passed = passed,
                Failed = failed,
                Ignored = ignored,
                Tests = tests.Select(t => new TestResultEntity
                {
                    ClassName = t.ClassName,
                    MethodName = t.MethodName,
                    Status = t.Status,
                    DurationMs = t.Duration.TotalMilliseconds,
                    Details = t.Details,
                }).ToList(),
            };

            assemblyEntities.Add(assemblyEntity);
        }

        var entity = new TestRunEntity
        {
            Id = runId,
            StartedAt = startedAt,
            CompletedAt = startedAt.AddMilliseconds(stopwatch.Elapsed.TotalMilliseconds),
            Passed = runResult.Passed,
            Failed = runResult.Failed,
            Ignored = runResult.Ignored,
            TotalDurationMs = runResult.TotalDuration.TotalMilliseconds,
            Assemblies = assemblyEntities,
        };

        this.dbContext.TestRuns.Add(entity);
        await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entity;
    }

    private static (int Passed, int Failed, int Ignored) Summarize(IEnumerable<TestResult> tests)
    {
        var passed = 0;
        var failed = 0;
        var ignored = 0;

        foreach (var test in tests)
        {
            switch (test.Status)
            {
                case TestStatus.Passed:
                    passed++;
                    break;
                case TestStatus.Failed:
                    failed++;
                    break;
                case TestStatus.Ignored:
                    ignored++;
                    break;
            }
        }

        return (passed, failed, ignored);
    }
}
