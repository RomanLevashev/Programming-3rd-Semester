using Microsoft.EntityFrameworkCore;
using MyNUnit.Core;
using MyNUnit.Web.Api.Persistence.Entities;

namespace MyNUnit.Web.Api.Persistence;

/// <summary>
/// EF Core database context for MyNUnit web data.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the uploaded assemblies table.
    /// </summary>
    public DbSet<UploadedAssembly> UploadedAssemblies => this.Set<UploadedAssembly>();

    /// <summary>
    /// Gets the test runs table.
    /// </summary>
    public DbSet<TestRunEntity> TestRuns => this.Set<TestRunEntity>();

    /// <summary>
    /// Gets the per-assembly results table.
    /// </summary>
    public DbSet<AssemblyRunEntity> AssemblyRuns => this.Set<AssemblyRunEntity>();

    /// <summary>
    /// Gets the per-test results table.
    /// </summary>
    public DbSet<TestResultEntity> TestResults => this.Set<TestResultEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UploadedAssembly>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<TestRunEntity>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<TestRunEntity>()
            .HasMany(x => x.Assemblies)
            .WithOne(x => x.TestRun)
            .HasForeignKey(x => x.TestRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssemblyRunEntity>()
            .HasMany(x => x.Tests)
            .WithOne(x => x.AssemblyRun)
            .HasForeignKey(x => x.AssemblyRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestResultEntity>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<TestResultEntity>()
            .Property(x => x.DurationMs)
            .HasPrecision(18, 3);

        modelBuilder.Entity<AssemblyRunEntity>()
            .Property(x => x.DurationMs)
            .HasPrecision(18, 3);

        modelBuilder.Entity<TestRunEntity>()
            .Property(x => x.TotalDurationMs)
            .HasPrecision(18, 3);
    }
}
