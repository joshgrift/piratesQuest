using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PiratesQuest.Server.Data;

namespace PiratesQuest.Server.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ServerRuntimeEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task Heartbeat_UpdatesRuntimeFields_AndServersEndpointShowsOnline()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var heartbeatRequest = TestHttpHelpers.CreateJsonRequest(HttpMethod.Post, $"/api/server/{serverId}/heartbeat", new
        {
            playerCount = 5,
            playerMax = 12,
            serverVersion = "0.7.0-alpha"
        });
        heartbeatRequest.WithServerKey();

        var heartbeatResponse = await fixture.Client.SendAsync(heartbeatRequest);
        heartbeatResponse.EnsureSuccessStatusCode();

        var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/servers");
        listRequest.WithBearerToken(adminToken);
        var listResponse = await fixture.Client.SendAsync(listRequest);

        using var doc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var server = doc.RootElement.EnumerateArray().Single();

        server.GetProperty("status").GetString().Should().Be("online");
        server.GetProperty("playerCount").GetInt32().Should().Be(5);
        server.GetProperty("playerMax").GetInt32().Should().Be(12);
        server.GetProperty("serverVersion").GetString().Should().Be("0.7.0-alpha");
    }

    [Fact]
    public async Task Heartbeat_NormalizesPlayerCountsAndBlankVersion()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var heartbeatRequest = TestHttpHelpers.CreateJsonRequest(HttpMethod.Post, $"/api/server/{serverId}/heartbeat", new
        {
            playerCount = 99,
            playerMax = 8,
            serverVersion = "  "
        });
        heartbeatRequest.WithServerKey();

        var response = await fixture.Client.SendAsync(heartbeatRequest);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("playerCount").GetInt32().Should().Be(8);
        doc.RootElement.GetProperty("serverVersion").GetString().Should().Be("unknown");
    }

    [Fact]
    public async Task ServersEndpoint_MarksServerOffline_WhenHeartbeatIsOld()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var server = await db.GameServers.SingleAsync(s => s.Id == serverId);
            server.LastSeenUtc = DateTime.UtcNow.AddMinutes(-10);
            await db.SaveChangesAsync();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/servers");
        request.WithBearerToken(adminToken);

        var response = await fixture.Client.SendAsync(request);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var serverJson = doc.RootElement.EnumerateArray().Single();

        serverJson.GetProperty("status").GetString().Should().Be("offline");
    }

    [Fact]
    public async Task Presence_WithoutServerKey_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();

        var response = await fixture.Client.PostAsJsonAsync("/api/server/1/presence", new
        {
            username = "jack",
            isOnline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
