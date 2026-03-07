namespace PiratesQuest.Data;

using System.Text.Json.Serialization;

// ── Godot -> Menu WebView state DTOs ───────────────────────────────

/// <summary>
/// Full state snapshot for the main menu webview.
/// Godot sends this whenever data changes.
/// </summary>
public record MenuStateDto
{
  public string ApiBaseUrl { get; init; } = "";
  public string Version { get; init; } = "";
  public string Username { get; init; } = "";
  public bool IsAuthenticated { get; init; }
  public string StatusMessage { get; init; } = "";
  public string StatusTone { get; init; } = "info";
  public bool IsAuthenticating { get; init; }
  public bool IsBackgroundMuted { get; init; }
  public MenuServerListingDto[] Servers { get; init; } = [];
}

/// <summary>
/// A server row shown in the menu webview.
/// </summary>
public record MenuServerListingDto
{
  public string ServerName { get; init; } = "";
  public string Description { get; init; } = "";
  public string IpAddress { get; init; } = "";
  public int Port { get; init; }
  public int PlayerCount { get; init; }
  public int PlayerMax { get; init; } = 8;
  public string Status { get; init; } = "offline";
  public string ServerVersion { get; init; } = "unknown";
}

// ── Menu WebView -> Godot IPC messages ─────────────────────────────

[JsonPolymorphic(TypeDiscriminatorPropertyName = "action")]
[JsonDerivedType(typeof(MenuReadyMessage), "ready")]
[JsonDerivedType(typeof(MenuLoginMessage), "login")]
[JsonDerivedType(typeof(MenuSignupMessage), "signup")]
[JsonDerivedType(typeof(MenuLogoutMessage), "logout")]
[JsonDerivedType(typeof(MenuRefreshServersMessage), "refresh_servers")]
[JsonDerivedType(typeof(MenuJoinServerMessage), "join_server")]
[JsonDerivedType(typeof(MenuSetBackgroundMutedMessage), "set_background_muted")]
[JsonDerivedType(typeof(MenuOpenUrlMessage), "open_url")]
public record MenuIpcMessage;

public record MenuReadyMessage : MenuIpcMessage;

public record MenuLoginMessage : MenuIpcMessage
{
  public string Username { get; init; } = "";
  public string Password { get; init; } = "";
}

public record MenuSignupMessage : MenuIpcMessage
{
  public string Username { get; init; } = "";
  public string Password { get; init; } = "";
}

public record MenuLogoutMessage : MenuIpcMessage;

public record MenuRefreshServersMessage : MenuIpcMessage;

public record MenuJoinServerMessage : MenuIpcMessage
{
  public string IpAddress { get; init; } = "";
  public int Port { get; init; }
}

public record MenuSetBackgroundMutedMessage : MenuIpcMessage
{
  public bool Muted { get; init; }
}

public record MenuOpenUrlMessage : MenuIpcMessage
{
  public string Url { get; init; } = "";
}
