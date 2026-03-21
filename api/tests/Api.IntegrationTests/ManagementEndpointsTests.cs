using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace PiratesQuest.Server.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ManagementEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task ManagementUsers_NonAdminUser_IsForbidden()
    {
        await fixture.ResetDatabaseAsync();

        await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var playerToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "player", "pw");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/management/users");
        request.WithBearerToken(playerToken);

        var response = await fixture.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanDeleteServer()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/management/server/{serverId}");
        deleteRequest.WithBearerToken(adminToken);

        var deleteResponse = await fixture.Client.SendAsync(deleteRequest);
        deleteResponse.EnsureSuccessStatusCode();

        var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/management/servers");
        listRequest.WithBearerToken(adminToken);

        var listResponse = await fixture.Client.SendAsync(listRequest);
        var payload = await listResponse.Content.ReadAsStringAsync();

        payload.Should().NotContain("Blackwake");
    }

    [Fact]
    public async Task Admin_CanPromoteUser_AndUserCanAccessAdminEndpointsAfterRelogin()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "modCandidate", "pw");

        var usersRequest = new HttpRequestMessage(HttpMethod.Get, "/api/management/users");
        usersRequest.WithBearerToken(adminToken);
        var usersResponse = await fixture.Client.SendAsync(usersRequest);
        using var usersDoc = JsonDocument.Parse(await usersResponse.Content.ReadAsStringAsync());
        var modCandidateId = usersDoc.RootElement
            .EnumerateArray()
            .Single(user => user.GetProperty("username").GetString() == "modCandidate")
            .GetProperty("id")
            .GetInt32();

        var promoteRequest = TestHttpHelpers.CreateJsonRequest(HttpMethod.Put, $"/api/management/user/{modCandidateId}/role", new { role = "Admin" });
        promoteRequest.WithBearerToken(adminToken);
        var promoteResponse = await fixture.Client.SendAsync(promoteRequest);
        promoteResponse.EnsureSuccessStatusCode();

        // JWT stores role at token creation time, so the user must log in again.
        var reloginResponse = await fixture.Client.PostAsJsonAsync("/api/login", new
        {
            username = "modCandidate",
            password = "pw"
        });
        reloginResponse.EnsureSuccessStatusCode();

        using var reloginDoc = JsonDocument.Parse(await reloginResponse.Content.ReadAsStringAsync());
        var promotedUserToken = reloginDoc.RootElement.GetProperty("token").GetString();

        var promotedUsersRequest = new HttpRequestMessage(HttpMethod.Get, "/api/management/users");
        promotedUsersRequest.WithBearerToken(promotedUserToken!);

        var promotedUsersResponse = await fixture.Client.SendAsync(promotedUsersRequest);
        promotedUsersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_CanClearSavedStateForUserOnServer()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var putStateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/server/{serverId}/state/scarlett")
        {
            Content = new StringContent("{\"gold\":99}", Encoding.UTF8, "application/json")
        };
        putStateRequest.WithServerKey();

        var putStateResponse = await fixture.Client.SendAsync(putStateRequest);
        putStateResponse.EnsureSuccessStatusCode();

        var clearStateRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/management/server/{serverId}/state/scarlett");
        clearStateRequest.WithBearerToken(adminToken);

        var clearStateResponse = await fixture.Client.SendAsync(clearStateRequest);
        clearStateResponse.EnsureSuccessStatusCode();

        var getStateRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/server/{serverId}/state/scarlett");
        getStateRequest.WithServerKey();

        var getStateResponse = await fixture.Client.SendAsync(getStateRequest);
        getStateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_ClearSavedState_WhenMissing_ReturnsNotFound()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");
        var serverId = await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken);

        var clearStateRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/management/server/{serverId}/state/missing-user");
        clearStateRequest.WithBearerToken(adminToken);

        var clearStateResponse = await fixture.Client.SendAsync(clearStateRequest);
        clearStateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
