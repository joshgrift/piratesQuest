using System;
using System.Net.Http;
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

  public static Task<(bool Success, string Token, string ErrorMessage)> LoginAsync(string username, string password)
  {
    return AuthenticateAsync("/api/login", username, password);
  }

  public static Task<(bool Success, string Token, string ErrorMessage)> SignupAsync(string username, string password)
  {
    return AuthenticateAsync("/api/signup", username, password);
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
