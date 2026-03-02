using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PiratesQuest.Server.Services;

/// <summary>
/// Sends simple text updates to a Discord channel using a bot token.
/// If config is missing, calls become a no-op.
/// </summary>
public sealed class DiscordNotifier
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordNotifier> _logger;

    public DiscordNotifier(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DiscordNotifier> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyPlayerPresenceAsync(string serverName, string username, bool isOnline)
    {
        var botToken = _configuration["DiscordBot:Token"];
        var channelId = _configuration["DiscordBot:ChannelId"];

        // Keep this optional so local dev still works without Discord credentials.
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(channelId))
            return;

        var content = isOnline
            ? $"{username} has sailed into {serverName}, beware!"
            : $"{username} has sailed out of {serverName}.";

        var payloadJson = JsonSerializer.Serialize(new { content });
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://discord.com/api/v10/channels/{channelId}/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Discord notification failed with status {StatusCode}. Response: {Response}",
                (int)response.StatusCode,
                responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Discord notification threw an exception.");
        }
    }
}
