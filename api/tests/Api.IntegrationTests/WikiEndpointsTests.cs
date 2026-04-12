using System.Net;
using FluentAssertions;

namespace PiratesQuest.Server.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class WikiEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task WikiHome_IsPublic_AndRendersMarkdownContent()
    {
        await fixture.ResetDatabaseAsync();

        var response = await fixture.Client.GetAsync("/wiki");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Pirate's Quest Wiki");
        html.Should().Contain("Getting Started");
    }

    [Fact]
    public async Task WikiSlugPage_RendersExpectedPage()
    {
        await fixture.ResetDatabaseAsync();

        var response = await fixture.Client.GetAsync("/wiki/ships-and-components");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Ships and Components");
        html.Should().Contain("Brigantine");
    }

    [Fact]
    public async Task WikiMissingPage_ReturnsNotFound()
    {
        await fixture.ResetDatabaseAsync();

        var response = await fixture.Client.GetAsync("/wiki/not-a-real-page");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
