using Xunit;

namespace PiratesQuest.Server.Api.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiTestFixture>
{
    public const string Name = "api-integration";
}
