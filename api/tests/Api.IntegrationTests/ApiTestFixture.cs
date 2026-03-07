using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace PiratesQuest.Server.Api.IntegrationTests;

public sealed class ApiTestFixture : IAsyncLifetime
{
    public const string JwtKey = "integration-tests-jwt-key-12345678901234567890";
    public const string ServerApiKey = "integration-test-server-key";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("piratesquest_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private PiratesQuestApiFactory _factory = null!;
    private Respawner _respawner = null!;

    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Set config through environment variables so the app sees test values
        // as soon as Program.cs starts building configuration.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("ServerApiKey", ServerApiKey);
        Environment.SetEnvironmentVariable("DiscordBot__Token", string.Empty);
        Environment.SetEnvironmentVariable("DiscordBot__ChannelId", string.Empty);

        _factory = new PiratesQuestApiFactory(_postgres.GetConnectionString());

        // First client creation boots the app and runs EF migrations.
        Client = _factory.CreateClient();

        await InitializeRespawnerAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__Default", null);
        Environment.SetEnvironmentVariable("Jwt__Key", null);
        Environment.SetEnvironmentVariable("ServerApiKey", null);
        Environment.SetEnvironmentVariable("DiscordBot__Token", null);
        Environment.SetEnvironmentVariable("DiscordBot__ChannelId", null);
    }

    private async Task InitializeRespawnerAsync()
    {
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    private sealed class PiratesQuestApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = connectionString,
                    ["Jwt:Key"] = JwtKey,
                    ["ServerApiKey"] = ServerApiKey,
                    ["DiscordBot:Token"] = string.Empty,
                    ["DiscordBot:ChannelId"] = string.Empty
                });
            });
        }
    }
}
