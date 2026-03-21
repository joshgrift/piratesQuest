using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PiratesQuest.Server.Services;

namespace PiratesQuest.Server.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class LeaderboardEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task LeaderboardEndpoint_ReturnsRankedGoldTotalsForOneServer()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);
        var otherServerId = await TestHttpHelpers.CreateServerAsAdminAsync(
            fixture.Client,
            adminToken,
            name: "Deadman's Reef",
            port: 7778);

        await PutStateAsync(serverId, "anne", """
            {
              "inventory": { "Coin": 75, "Wood": 3 },
              "vault": { "items": { "Coin": 200 } }
            }
            """);

        await PutStateAsync(serverId, "blackbeard", """
            {
              "inventory": { "Coin": 300 },
              "vault": { "items": { "Coin": 50 } }
            }
            """);

        await PutStateAsync(otherServerId, "other-shard-player", """
            {
              "inventory": { "Coin": 9999 }
            }
            """);

        await RefreshLeaderboardAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/server/{serverId}/leaderboard");
        request.WithServerKey();

        var response = await fixture.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = doc.RootElement.EnumerateArray().ToArray();

        rows.Should().HaveCount(2);
        rows[0].GetProperty("userId").GetString().Should().Be("blackbeard");
        rows[0].GetProperty("inventoryGold").GetInt32().Should().Be(300);
        rows[0].GetProperty("vaultGold").GetInt32().Should().Be(50);
        rows[0].GetProperty("totalGold").GetInt32().Should().Be(350);

        rows[1].GetProperty("userId").GetString().Should().Be("anne");
        rows[1].GetProperty("inventoryGold").GetInt32().Should().Be(75);
        rows[1].GetProperty("vaultGold").GetInt32().Should().Be(200);
        rows[1].GetProperty("totalGold").GetInt32().Should().Be(275);
    }

    [Fact]
    public async Task LeaderboardRefresh_RemovesRowsWhenSavedStateIsDeleted()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        await PutStateAsync(serverId, "scarlett", """
            {
              "inventory": { "Coin": 123 },
              "vault": { "items": { "Coin": 456 } }
            }
            """);

        await RefreshLeaderboardAsync();

        var clearStateRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/management/server/{serverId}/state/scarlett");
        clearStateRequest.WithBearerToken(adminToken);
        var clearStateResponse = await fixture.Client.SendAsync(clearStateRequest);
        clearStateResponse.EnsureSuccessStatusCode();

        await RefreshLeaderboardAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/server/{serverId}/leaderboard");
        request.WithServerKey();

        var response = await fixture.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task LeaderboardEndpoint_WithoutServerKey_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();

        var response = await fixture.Client.GetAsync("/api/server/1/leaderboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task RefreshLeaderboardAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var hostedServices = scope.ServiceProvider.GetServices<IHostedService>();
        var refreshService = hostedServices.OfType<LeaderboardRefreshService>().Single();
        await refreshService.RefreshLeaderboardAsync();
    }

    private async Task PutStateAsync(int serverId, string userId, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/server/{serverId}/state/{userId}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.WithServerKey();

        var response = await fixture.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
