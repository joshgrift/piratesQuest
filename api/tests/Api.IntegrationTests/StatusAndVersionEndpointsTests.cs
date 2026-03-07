using System.Text.Json;
using FluentAssertions;

namespace PiratesQuest.Server.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class StatusAndVersionEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task Status_DefaultsToUnknownVersion_WhenNotSet()
    {
        await fixture.ResetDatabaseAsync();

        var response = await fixture.Client.GetAsync("/api/status");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("version").GetString().Should().Be("unknown");
    }

    [Fact]
    public async Task Status_ReturnsServersSortedByName()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");

        await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken, name: "Zulu", port: 7001);
        await TestHttpHelpers.CreateServerAsAdminAsync(fixture.Client, adminToken, name: "Alpha", port: 7002);

        var response = await fixture.Client.GetAsync("/api/status");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = doc.RootElement.GetProperty("servers")
            .EnumerateArray()
            .Select(s => s.GetProperty("name").GetString())
            .ToArray();

        names.Should().ContainInOrder("Alpha", "Zulu");
    }

    [Fact]
    public async Task Admin_CanSetVersion_AndStatusReturnsIt()
    {
        await fixture.ResetDatabaseAsync();

        var adminToken = await TestHttpHelpers.SignupAndGetTokenAsync(fixture.Client, "admin", "pw");

        var setVersion = TestHttpHelpers.CreateJsonRequest(HttpMethod.Post, "/api/management/version", new
        {
            version = "0.6.0-alpha"
        });
        setVersion.WithBearerToken(adminToken);

        var setResponse = await fixture.Client.SendAsync(setVersion);
        setResponse.EnsureSuccessStatusCode();

        var statusResponse = await fixture.Client.GetAsync("/api/status");
        statusResponse.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("version").GetString().Should().Be("0.6.0-alpha");
    }
}
