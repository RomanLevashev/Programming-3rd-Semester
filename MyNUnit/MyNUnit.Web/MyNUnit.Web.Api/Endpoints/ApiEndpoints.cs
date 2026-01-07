// <copyright file="ApiEndpoints.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Web.Api.Endpoints;

using Microsoft.EntityFrameworkCore;
using MyNUnit.Web.Api.Contracts;
using MyNUnit.Web.Api.Extensions;
using MyNUnit.Web.Api.Persistence;
using MyNUnit.Web.Api.Persistence.Entities;
using MyNUnit.Web.Api.Services;

/// <summary>
/// Defines API endpoint mappings for the application.
/// </summary>
public static class ApiEndpoints
{
    /// <summary>
    /// Registers all HTTP endpoints for the API.
    /// </summary>
    /// <param name="app">Web application instance.</param>
    public static void MapApi(this WebApplication app)
    {
        app.MapPost("/api/assemblies", UploadAssemblies);
        app.MapGet("/api/assemblies", ListAssemblies);
        app.MapDelete("/api/assemblies/{id:guid}", DeleteAssembly);
        app.MapDelete("/api/assemblies", DeleteAllAssemblies);
        app.MapPost("/api/runs", RunTests);
        app.MapGet("/api/runs", ListRuns);
        app.MapGet("/api/runs/{id:guid}", GetRunDetails);
        app.MapDelete("/api/runs", ClearRuns);
    }

    private static async Task<IResult> UploadAssemblies(
        HttpRequest request,
        AppDbContext dbContext,
        AssemblyStorage storage,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Ожидается multipart/form-data с файлами.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var files = form.Files;

        if (files.Count == 0)
        {
            return Results.BadRequest("Не найдено файлов для загрузки.");
        }

        var stored = new List<AssemblyUploadDto>();
        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                continue;
            }

            var entity = await storage.SaveAsync(file, cancellationToken);
            dbContext.UploadedAssemblies.Add(entity);
            stored.Add(entity.ToDto());
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(stored);
    }

    private static async Task<IResult> ListAssemblies(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var assemblies = await dbContext.UploadedAssemblies
            .ToListAsync(cancellationToken);

        var ordered = assemblies
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => x.ToDto())
            .ToList();

        return Results.Ok(ordered);
    }

    private static async Task<IResult> DeleteAssembly(
        Guid id,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.UploadedAssemblies
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            return Results.NotFound();
        }

        TryDeleteFile(entity.StoredPath);
        dbContext.UploadedAssemblies.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }

    private static async Task<IResult> DeleteAllAssemblies(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var assemblies = await dbContext.UploadedAssemblies.ToListAsync(cancellationToken);
        foreach (var assembly in assemblies)
        {
            TryDeleteFile(assembly.StoredPath);
        }

        dbContext.UploadedAssemblies.RemoveRange(assemblies);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }

    private static async Task<IResult> RunTests(
        RunRequest? request,
        AppDbContext dbContext,
        TestExecutionService executor,
        CancellationToken cancellationToken)
    {
        var availableIds = await dbContext.UploadedAssemblies
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (availableIds.Count == 0)
        {
            return Results.BadRequest("Нет загруженных сборок для запуска. Сначала загрузите хотя бы одну сборку.");
        }

        var targetIds = request?.AssemblyIds?.Count > 0
            ? availableIds.Intersect(request.AssemblyIds!).ToList()
            : availableIds;

        if (targetIds.Count == 0)
        {
            return Results.BadRequest("Ни одна из указанных сборок не найдена.");
        }

        TestRunEntity run;
        try
        {
            run = await executor.RunAsync(targetIds, cancellationToken);
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Не удалось выполнить тесты: {ex.Message}");
        }

        return Results.Ok(run.ToDetailsDto());
    }

    private static async Task<IResult> ListRuns(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var runs = await dbContext.TestRuns
            .ToListAsync(cancellationToken);

        var ordered = runs
            .OrderByDescending(x => x.StartedAt)
            .Select(x => x.ToSummaryDto())
            .ToList();

        return Results.Ok(ordered);
    }

    private static async Task<IResult> ClearRuns(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var runs = await dbContext.TestRuns.ToListAsync(cancellationToken);
        dbContext.TestRuns.RemoveRange(runs);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> GetRunDetails(
        Guid id,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.TestRuns
            .Include(x => x.Assemblies)
            .ThenInclude(x => x.Tests)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (run == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(run.ToDetailsDto());
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
