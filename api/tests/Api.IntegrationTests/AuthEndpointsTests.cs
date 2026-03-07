using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace PiratesQuest.Server.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task Signup_FirstUserCanAccessAdminEndpoints()
    {
        await fixture.ResetDatabaseAsync();

        var token = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "captain", "secret");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/management/users");
        request.WithBearerToken(token);

        var response = await fixture.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Signup_WithDuplicateUsername_ReturnsConflict()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Client.PostAsJsonAsync("/api/signup", new { username = "scarlett", password = "pw" });
        var duplicateResponse = await fixture.Client.PostAsJsonAsync("/api/signup", new { username = "scarlett", password = "pw" });

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();

        await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "anne", "good-password");

        var response = await fixture.Client.PostAsJsonAsync("/api/login", new
        {
            username = "anne",
            password = "bad-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ServersEndpoint_WithoutJwt_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();

        var response = await fixture.Client.GetAsync("/api/servers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
