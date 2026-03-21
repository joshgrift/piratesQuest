using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PiratesQuest.Server.Data;
using PiratesQuest.Server.Services;

namespace PiratesQuest.Server.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ServerOfflineMonitorTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task OfflineMonitor_DoesNotAlert_WhenServerStartsOffline()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);
        await SetServerLastSeenUtcAsync(serverId, DateTime.UtcNow.AddMinutes(-10));

        var notifier = new FakeDiscordNotifier();
        var monitor = CreateMonitor(notifier);

        await monitor.CheckServersOnceAsync();

        notifier.OfflineNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task OfflineMonitor_AlertsOnce_WhenServerTransitionsOffline()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var notifier = new FakeDiscordNotifier();
        var monitor = CreateMonitor(notifier);

        await SendHeartbeatAsync(serverId, playerCount: 2, playerMax: 8, serverVersion: "0.7.1");
        await monitor.CheckServersOnceAsync();
        await SetServerLastSeenUtcAsync(serverId, DateTime.UtcNow.AddMinutes(-10));

        await monitor.CheckServersOnceAsync();
        await monitor.CheckServersOnceAsync();

        notifier.OfflineNotifications.Should().Equal("Blackwake");
    }

    [Fact]
    public async Task OfflineMonitor_AlertsAgain_AfterServerRecovers_ThenGoesOfflineAgain()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var notifier = new FakeDiscordNotifier();
        var monitor = CreateMonitor(notifier);

        await SendHeartbeatAsync(serverId, playerCount: 2, playerMax: 8, serverVersion: "0.7.1");
        await monitor.CheckServersOnceAsync();
        await SetServerLastSeenUtcAsync(serverId, DateTime.UtcNow.AddMinutes(-10));
        await monitor.CheckServersOnceAsync();

        await SendHeartbeatAsync(serverId, playerCount: 1, playerMax: 8, serverVersion: "0.7.1");
        await monitor.CheckServersOnceAsync();

        await SetServerLastSeenUtcAsync(serverId, DateTime.UtcNow.AddMinutes(-10));
        await monitor.CheckServersOnceAsync();

        notifier.OfflineNotifications.Should().Equal("Blackwake", "Blackwake");
    }

    [Fact]
    public async Task OfflineMonitor_IgnoresInactiveServers()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var notifier = new FakeDiscordNotifier();
        var monitor = CreateMonitor(notifier);

        await SendHeartbeatAsync(serverId, playerCount: 2, playerMax: 8, serverVersion: "0.7.1");
        await monitor.CheckServersOnceAsync();
        await SetServerActiveAsync(serverId, isActive: false);
        await SetServerLastSeenUtcAsync(serverId, DateTime.UtcNow.AddMinutes(-10));

        await monitor.CheckServersOnceAsync();

        notifier.OfflineNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscordNotifier_OfflineAlerts_FallBackToDefaultChannel()
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DiscordBot:Token"] = "test-token",
                ["DiscordBot:ChannelId"] = "default-channel",
                ["DiscordBot:ActivityChannelId"] = ""
            })
            .Build();
        var notifier = new DiscordNotifier(
            httpClient,
            configuration,
            NullLogger<DiscordNotifier>.Instance);

        await notifier.NotifyServerOfflineAsync("Blackwake");

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.ToString()
            .Should().Be("https://discord.com/api/v10/channels/default-channel/messages");
    }

    private ServerOfflineMonitor CreateMonitor(FakeDiscordNotifier notifier)
    {
        var scopeFactory = fixture.Services.GetRequiredService<IServiceScopeFactory>();
        return new ServerOfflineMonitor(
            scopeFactory,
            notifier,
            NullLogger<ServerOfflineMonitor>.Instance);
    }

    private async Task SetServerLastSeenUtcAsync(int serverId, DateTime? lastSeenUtc)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var server = await db.GameServers.SingleAsync(server => server.Id == serverId);
        server.LastSeenUtc = lastSeenUtc;
        await db.SaveChangesAsync();
    }

    private async Task SetServerActiveAsync(int serverId, bool isActive)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var server = await db.GameServers.SingleAsync(server => server.Id == serverId);
        server.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    private async Task SendHeartbeatAsync(int serverId, int playerCount, int playerMax, string serverVersion)
    {
        var request = TestHttpHelpers.CreateJsonRequest(HttpMethod.Post, $"/api/server/{serverId}/heartbeat", new
        {
            playerCount,
            playerMax,
            serverVersion
        });
        request.WithServerKey();

        var response = await fixture.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private sealed class FakeDiscordNotifier : IDiscordNotifier
    {
        public List<string> OfflineNotifications { get; } = [];

        public Task NotifyPlayerPresenceAsync(string serverName, string username, bool isOnline, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task NotifyServerOfflineAsync(string serverName, CancellationToken cancellationToken = default)
        {
            OfflineNotifications.Add(serverName);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = request.Content is null
                    ? null
                    : new StringContent(
                        request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult(),
                        Encoding.UTF8,
                        request.Content.Headers.ContentType?.MediaType)
            };

            Requests.Add(clone);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
