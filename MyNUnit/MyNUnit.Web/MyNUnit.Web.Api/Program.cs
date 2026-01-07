// <copyright file="Program.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using MyNUnit.Web.Api.Endpoints;
using MyNUnit.Web.Api.Persistence;
using MyNUnit.Web.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 512 * 1024 * 1024);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "AppData");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "mynunit.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<AssemblyStorage>();
builder.Services.AddScoped<TestExecutionService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

app.MapApi();

app.Run();

/// <summary>
/// Application entry point for WebApplicationFactory integration tests.
/// </summary>
public partial class Program
{
}
