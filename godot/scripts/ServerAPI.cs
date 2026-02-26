using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// HTTP client used by the Godot game server to call the REST API.
/// Authenticates with X-Server-Key (shared secret), not user JWTs.
/// </summary>
public static class ServerAPI
{
  private static readonly System.Net.Http.HttpClient HttpClient = new();

  public static async Task<bool> SavePlayerStateAsync(int serverId, string userId, string stateJson)
  {
    try
    {
      var url = $"{Configuration.ApiBaseUrl}/api/server/{serverId}/state/{userId}";
      var request = new HttpRequestMessage(HttpMethod.Put, url)
      {
        Content = new StringContent(stateJson, Encoding.UTF8, "application/json")
      };
      request.Headers.Add("X-Server-Key", Configuration.ServerApiKey);

      using var response = await HttpClient.SendAsync(request);
      if (!response.IsSuccessStatusCode)
      {
        GD.PrintErr($"Failed to save state for user {userId}: HTTP {(int)response.StatusCode}");
        return false;
      }

      GD.Print($"Saved state for user {userId}");
      return true;
    }
    catch (Exception ex)
    {
      GD.PrintErr($"Exception saving state for user {userId}: {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Loads a player's saved state from the API.
  /// Returns (stateJson, isError):
  ///   - (json, false)  = state loaded successfully
  ///   - (null, false)  = no saved state exists (404) — player starts fresh
  ///   - (null, true)   = API unreachable or returned an error
  /// </summary>
  public static async Task<(string StateJson, bool IsError)> LoadPlayerStateAsync(int serverId, string userId)
  {
    try
    {
      var url = $"{Configuration.ApiBaseUrl}/api/server/{serverId}/state/{userId}";
      var request = new HttpRequestMessage(HttpMethod.Get, url);
      request.Headers.Add("X-Server-Key", Configuration.ServerApiKey);

      using var response = await HttpClient.SendAsync(request);
      if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        GD.Print($"No saved state for user {userId}");
        return (null, false);
      }

      if (!response.IsSuccessStatusCode)
      {
        GD.PrintErr($"Failed to load state for user {userId}: HTTP {(int)response.StatusCode}");
        return (null, true);
      }

      var json = await response.Content.ReadAsStringAsync();
      GD.Print($"Loaded state for user {userId}");
      return (json, false);
    }
    catch (Exception ex)
    {
      GD.PrintErr($"Exception loading state for user {userId}: {ex.Message}");
      return (null, true);
    }
  }
}
