using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyNUnit.Web.Api.Persistence;

namespace MyNUnit.Web.Api.Tests;

public sealed class ApiEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient client;

    public ApiEndpointsTests(ApiWebApplicationFactory factory)
    {
        this.client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAssemblies_ReturnsEmptyList()
    {
        var response = await this.client.GetAsync("/api/assemblies");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", content.Trim());
    }

    [Fact]
    public async Task RunTests_WithoutAssemblies_ReturnsBadRequest()
    {
        var response = await this.client.PostAsync(
            "/api/runs",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAndDeleteAssembly_Works()
    {
        var content = new MultipartFormDataContent();
        var fileBytes = Encoding.UTF8.GetBytes("fake");
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "files", "sample.dll");

        var uploadResponse = await this.client.PostAsync("/api/assemblies", content);
        uploadResponse.EnsureSuccessStatusCode();

        var json = await uploadResponse.Content.ReadAsStringAsync();
        Assert.Contains("sample.dll", json);

        var listResponse = await this.client.GetAsync("/api/assemblies");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("sample.dll", listJson);

        using var doc = System.Text.Json.JsonDocument.Parse(listJson);
        var id = doc.RootElement[0].GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(id));

        var deleteResponse = await this.client.DeleteAsync($"/api/assemblies/{id}");
        deleteResponse.EnsureSuccessStatusCode();

        var listAfterDelete = await this.client.GetAsync("/api/assemblies");
        listAfterDelete.EnsureSuccessStatusCode();
        var listAfterJson = await listAfterDelete.Content.ReadAsStringAsync();
        Assert.Equal("[]", listAfterJson.Trim());
    }

    [Fact]
    public async Task ClearRuns_ReturnsOk()
    {
        var response = await this.client.DeleteAsync("/api/runs");
        response.EnsureSuccessStatusCode();
    }
}

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string contentRoot;

    public ApiWebApplicationFactory()
    {
        this.contentRoot = Path.Combine(Path.GetTempPath(), "mynunit-web-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.contentRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(this.contentRoot);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            var dbPath = Path.Combine(this.contentRoot, "test.db");
            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                if (Directory.Exists(this.contentRoot))
                {
                    Directory.Delete(this.contentRoot, true);
                }
            }
            catch
            {
            }
        }
    }
}
