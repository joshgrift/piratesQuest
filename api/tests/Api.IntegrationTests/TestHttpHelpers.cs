using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PiratesQuest.Server.Api.IntegrationTests;

public static class TestHttpHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<string> SignupAndGetTokenAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/signup", new { username, password });
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        return token!.Token;
    }

    public static HttpRequestMessage CreateJsonRequest(HttpMethod method, string url, object body)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        return request;
    }

    public static void WithBearerToken(this HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static void WithServerKey(this HttpRequestMessage request)
    {
        request.Headers.Add("X-Server-Key", ApiTestFixture.ServerApiKey);
    }

    public static async Task<int> CreateServerAsAdminAsync(
        HttpClient client,
        string adminToken,
        string name = "Blackwake",
        string address = "127.0.0.1",
        int port = 7777,
        string description = "Main shard")
    {
        var request = CreateJsonRequest(HttpMethod.Put, "/api/management/server", new
        {
            name,
            address,
            port,
            description
        });
        request.WithBearerToken(adminToken);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    private sealed record TokenResponse(string Token);
}
