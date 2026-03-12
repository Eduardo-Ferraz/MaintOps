using Industriall.MaintOps.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Industriall.MaintOps.Tests.Integration;

/// <summary>
/// Base class for integration tests. Spins up an ephemeral PostgreSQL container
/// via Testcontainers and replaces the application's DbContext with it.
/// </summary>
public abstract class IntegrationTestBase
    : IAsyncLifetime, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("maintops_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected HttpClient Client { get; private set; } = default!;
    protected WebApplicationFactory<Program> Factory  { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration.
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor is not null)
                        services.Remove(descriptor);

                    // Register DbContext pointing at the test container.
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });
            });

        // Ensure the schema is created for test runs.
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
