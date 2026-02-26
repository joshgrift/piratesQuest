using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

// Central place for API communication from the Godot client.
// Keep networking details here so UI code stays simple.
public static class API
{
  private static readonly System.Net.Http.HttpClient HttpClient = new();

  // Minimal DTO for login/signup request body.
  private sealed class AuthRequest
  {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }

  // Minimal DTO for token response from the server.
  private sealed class AuthResponse
  {
    public string Token { get; set; } = string.Empty;
  }

  // DTO from GET /api/servers
  private sealed class ServerResponse
  {
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
  }

  public static Task<(bool Success, string Token, string ErrorMessage)> LoginAsync(string username, string password)
  {
    return AuthenticateAsync("/api/login", username, password);
  }

  public static Task<(bool Success, string Token, string ErrorMessage)> SignupAsync(string username, string password)
  {
    return AuthenticateAsync("/api/signup", username, password);
  }

  public static async Task<(bool Success, ServerListingInfo[] Servers, string ErrorMessage, bool IsUnauthorized)> GetServerListingsAsync()
  {
    var token = Configuration.GetUserToken();
    if (string.IsNullOrWhiteSpace(token))
    {
      return (false, Array.Empty<ServerListingInfo>(), "Missing user token.", false);
    }

    try
    {
      var request = new HttpRequestMessage(HttpMethod.Get, $"{Configuration.ApiBaseUrl}/api/servers");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

      using var response = await HttpClient.SendAsync(request);
      if (!response.IsSuccessStatusCode)
      {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
          return (false, Array.Empty<ServerListingInfo>(), "Session expired. Please log in again.", true);
        }

        return (false, Array.Empty<ServerListingInfo>(), $"Could not load servers ({(int)response.StatusCode}).", false);
      }

      var responseText = await response.Content.ReadAsStringAsync();
      var rawServers = JsonSerializer.Deserialize<ServerResponse[]>(
        responseText,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      ) ?? Array.Empty<ServerResponse>();

      var servers = new ServerListingInfo[rawServers.Length];
      for (var i = 0; i < rawServers.Length; i++)
      {
        servers[i] = new ServerListingInfo
        {
          ServerName = rawServers[i].Name,
          IpAddress = rawServers[i].Address,
          Port = rawServers[i].Port
        };
      }

      return (true, servers, string.Empty, false);
    }
    catch (Exception exception)
    {
      GD.PrintErr($"API server list exception: {exception}");
      return (false, Array.Empty<ServerListingInfo>(), "Could not reach API server.", false);
    }
  }

  private static async Task<(bool Success, string Token, string ErrorMessage)> AuthenticateAsync(string endpointPath, string username, string password)
  {
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
      return (false, string.Empty, "Username and password are required.");
    }

    try
    {
      var requestBody = new AuthRequest
      {
        Username = username.Trim(),
        Password = password
      };

      var json = JsonSerializer.Serialize(requestBody);
      var content = new StringContent(json, Encoding.UTF8, "application/json");
      var url = $"{Configuration.ApiBaseUrl}{endpointPath}";

      using var response = await HttpClient.PostAsync(url, content);
      if (!response.IsSuccessStatusCode)
      {
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
          return (false, string.Empty, "Username already exists.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
          if (endpointPath == "/api/login")
          {
            return (false, string.Empty, "Invalid username or password. If you do not have an account, click Sign Up.");
          }

          return (false, string.Empty, "Unauthorized.");
        }

        return (false, string.Empty, $"Auth failed ({(int)response.StatusCode}).");
      }

      var responseText = await response.Content.ReadAsStringAsync();
      var authResponse = JsonSerializer.Deserialize<AuthResponse>(
        responseText,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      );

      if (authResponse == null || string.IsNullOrWhiteSpace(authResponse.Token))
      {
        return (false, string.Empty, "Server response did not include a token.");
      }

      return (true, authResponse.Token, string.Empty);
    }
    catch (Exception exception)
    {
      GD.PrintErr($"API auth exception: {exception}");
      return (false, string.Empty, "Could not reach API server.");
    }
  }
}
