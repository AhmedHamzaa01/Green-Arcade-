using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Common;
using Testcontainers.PostgreSql;

namespace RowCycle.Tests.Infrastructure;

/// <summary>Runs the API in memory against a real PostgreSQL container (one per test run).</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "AdminPass123";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public TestEmailSender Emails { get; } = new();

    public Task InitializeAsync() => _postgres.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting applies before Program.cs reads configuration; ConfigureAppConfiguration would be too late.
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-that-is-at-least-32-chars");
        builder.UseSetting("App:WebBaseUrl", "http://localhost:4200");
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);

        builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(Emails));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
