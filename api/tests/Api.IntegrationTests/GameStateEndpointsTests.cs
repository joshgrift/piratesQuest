using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace PiratesQuest.Server.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class GameStateEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task PutThenGetState_RoundTripsJsonPayload()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        const string body = "{\"gold\":123,\"inventory\":[\"fish\",\"wood\"]}";

        var put = new HttpRequestMessage(HttpMethod.Put, $"/api/server/{serverId}/state/user-1")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        put.WithServerKey();

        var putResponse = await fixture.Client.SendAsync(put);
        putResponse.EnsureSuccessStatusCode();

        var get = new HttpRequestMessage(HttpMethod.Get, $"/api/server/{serverId}/state/user-1");
        get.WithServerKey();

        var getResponse = await fixture.Client.SendAsync(get);
        var payload = await getResponse.Content.ReadAsStringAsync();

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var actualDoc = JsonDocument.Parse(payload);
        actualDoc.RootElement.GetProperty("gold").GetInt32().Should().Be(123);
        var inventory = actualDoc.RootElement.GetProperty("inventory").EnumerateArray().Select(i => i.GetString()).ToArray();
        inventory.Should().ContainInOrder("fish", "wood");
    }

    [Fact]
    public async Task PutState_WhenServerDoesNotExist_ReturnsNotFound()
    {
        await fixture.ResetDatabaseAsync();

        var put = new HttpRequestMessage(HttpMethod.Put, "/api/server/999/state/user-2")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        put.WithServerKey();

        var response = await fixture.Client.SendAsync(put);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetState_WhenMissing_ReturnsNotFound()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/server/{serverId}/state/unknown");
        request.WithServerKey();

        var response = await fixture.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetState_WithoutServerKey_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();

        var response = await fixture.Client.GetAsync("/api/server/1/state/user");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
