using System;
using System.Linq;
using Godot;

public record ServerListingInfo
{
  public string ServerName;
  public string IpAddress;
  public int Port;
}

partial class Configuration : Node
{
  private const string LocalApiBaseUrl = "http://localhost:5236";
  private const string ProductionApiBaseUrl = "https://pirates.quest";

  // Godot's per-user, writable save file. On each OS this maps to a safe local app-data folder.
  private const string LocalConfigPath = "user://settings.cfg";
  private const string AuthSection = "auth";
  private const string UserTokenKey = "user_token";
  private const string UsernameKey = "username";
  private const string GateScenePath = "res://scenes/menu/menu.tscn";

  private bool _isRedirectingToGate = false;
  private static string _pendingMenuError = string.Empty;

  private static string _memoryToken = string.Empty;
  private static string _memoryUsername = string.Empty;

  public static bool RandomSpawnEnabled { get; } = true;
  public static int StartingCoin { get; } = 100;
  public static bool IsCreative { get; private set; } = false;
  public static int DefaultPort { get; } = 7777;
  // Override with --api-url <url>.
  // Local/editor/debug builds default to localhost; non-local builds default to production.
  public static string ApiBaseUrl { get; private set; } = GetDefaultApiBaseUrl();

  public static int ServerId { get; private set; }
  public static string ServerApiKey { get; private set; }

  public static string CmdUser { get; private set; }
  public static string CmdPassword { get; private set; }
  public static bool DisableSaveUser { get; private set; }
  // Defaults to {ApiBaseUrl}/fragments/webview/. Override with --webview-url <url>.
  public static string WebViewUrl { get; private set; } = $"{ApiBaseUrl}/fragments/webview/";

  private static string GetDefaultApiBaseUrl()
  {
    // "editor" is true when running from Godot editor.
    // IsDebugBuild is true for local debug runs/exports.
    // Release exports will fall back to production.
    bool isLocalRuntime = OS.HasFeature("editor") || OS.IsDebugBuild();
    return isLocalRuntime ? LocalApiBaseUrl : ProductionApiBaseUrl;
  }

  public override void _Ready()
  {
    if (IsDesignatedServerMode())
    {
      try
      {
        ParseServerArgs();
      }
      catch (Exception exception)
      {
        GD.PushError($"Fatal server startup error: {exception.Message}");
        GetTree()?.Quit(1);
        return;
      }
    }
    else
    {
      ParseClientArgs();
    }
    CallDeferred(MethodName.ConfigureWindowTitle);
  }

  private static void ParseServerArgs()
  {
    bool hasServerId = false;
    bool hasApiKey = false;

    var args = OS.GetCmdlineArgs();
    for (int i = 0; i < args.Length - 1; i++)
    {
      switch (args[i])
      {
        case "--server-id":
          if (int.TryParse(args[i + 1], out int id))
          {
            ServerId = id;
            hasServerId = true;
          }
          break;
        case "--server-api-key":
          ServerApiKey = args[i + 1];
          hasApiKey = true;
          break;
        case "--api-url":
          ApiBaseUrl = args[i + 1].TrimEnd('/');
          break;
      }
    }

    if (!hasServerId || !hasApiKey)
      throw new InvalidOperationException(
        "Dedicated server requires --server-id <int> and --server-api-key <string> arguments");
  }

  private static void ParseClientArgs()
  {
    bool hasExplicitWebViewUrl = false;

    var args = OS.GetCmdlineArgs();
    for (int i = 0; i < args.Length; i++)
    {
      switch (args[i])
      {
        case "--user" when i + 1 < args.Length:
          CmdUser = args[i + 1];
          break;
        case "--password" when i + 1 < args.Length:
          CmdPassword = args[i + 1];
          break;
        case "--disableSaveUser":
          DisableSaveUser = true;
          break;
        case "--webview-url" when i + 1 < args.Length:
          WebViewUrl = args[i + 1];
          hasExplicitWebViewUrl = true;
          break;
        case "--api-url" when i + 1 < args.Length:
          ApiBaseUrl = args[i + 1].TrimEnd('/');
          break;
        case "--creative":
          IsCreative = true;
          break;
      }
    }

    // When --api-url is provided without an explicit --webview-url,
    // recalculate the webview URL from the new API base.
    // In production the webview is served from the same host as the API.
    if (!hasExplicitWebViewUrl)
    {
      WebViewUrl = $"{ApiBaseUrl}/fragments/webview/";
    }
  }

  public override void _Process(double delta)
  {
    EnforceLoginGate();
  }

  private void ConfigureWindowTitle()
  {
    String title = $"PiratesQuest {OS.GetCmdlineArgs().Join(" ")}";
    GD.Print($"setting title to {title}");
    DisplayServer.WindowSetTitle(title);
  }

  public static string GetVersion()
  {
    return ProjectSettings.GetSetting("application/config/version").AsString();
  }

  public static bool IsDesignatedServerMode()
  {
    var args = OS.GetCmdlineArgs();

    return args.Contains("--server") || OS.HasFeature("dedicated_server");
  }

  public static Error SaveUserToken(string userToken)
  {
    if (DisableSaveUser)
    {
      _memoryToken = userToken ?? string.Empty;
      return Error.Ok;
    }

    var config = new ConfigFile();
    var loadError = config.Load(LocalConfigPath);
    if (loadError != Error.Ok && loadError != Error.FileNotFound)
    {
      return loadError;
    }

    config.SetValue(AuthSection, UserTokenKey, userToken ?? string.Empty);
    return config.Save(LocalConfigPath);
  }

  public static string GetUserToken()
  {
    if (DisableSaveUser)
      return _memoryToken;

    var config = new ConfigFile();
    var loadError = config.Load(LocalConfigPath);
    if (loadError != Error.Ok)
    {
      return string.Empty;
    }

    return config.GetValue(AuthSection, UserTokenKey, string.Empty).AsString();
  }

  public static Error ClearUserToken()
  {
    if (DisableSaveUser)
    {
      _memoryToken = string.Empty;
      _memoryUsername = string.Empty;
      return Error.Ok;
    }

    var config = new ConfigFile();
    var loadError = config.Load(LocalConfigPath);
    if (loadError != Error.Ok && loadError != Error.FileNotFound)
    {
      return loadError;
    }

    config.SetValue(AuthSection, UserTokenKey, string.Empty);
    config.SetValue(AuthSection, UsernameKey, string.Empty);
    return config.Save(LocalConfigPath);
  }

  public static Error SaveUsername(string username)
  {
    if (DisableSaveUser)
    {
      _memoryUsername = username ?? string.Empty;
      return Error.Ok;
    }

    var config = new ConfigFile();
    var loadError = config.Load(LocalConfigPath);
    if (loadError != Error.Ok && loadError != Error.FileNotFound)
    {
      return loadError;
    }

    config.SetValue(AuthSection, UsernameKey, username ?? string.Empty);
    return config.Save(LocalConfigPath);
  }

  public static string GetUsername()
  {
    if (DisableSaveUser)
      return _memoryUsername;

    var config = new ConfigFile();
    var loadError = config.Load(LocalConfigPath);
    if (loadError != Error.Ok)
    {
      return string.Empty;
    }

    return config.GetValue(AuthSection, UsernameKey, string.Empty).AsString();
  }

  // A valid local session needs both token + username.
  public static bool HasValidSession()
  {
    return !string.IsNullOrWhiteSpace(GetUserToken()) && !string.IsNullOrWhiteSpace(GetUsername());
  }

  // Central auth bootstrap rules for the menu gate.
  // Menu should use this instead of deciding login validity itself.
  public static (bool ShouldAutoLogin, string Username, string StatusMessage) ResolveLoginGateState()
  {
    var token = GetUserToken();
    var username = GetUsername().Trim();

    // Happy path: we have everything needed to continue.
    if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(username))
    {
      return (true, username, string.Empty);
    }

    // Old/incomplete local state. Clear it in one place.
    if (!string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(username))
    {
      var clearError = ClearUserToken();
      if (clearError != Error.Ok)
      {
        return (false, string.Empty, $"Failed to reset login state: {clearError}");
      }

      return (false, string.Empty, "Please log in again.");
    }

    // Optional convenience: keep username pre-filled when token is missing.
    return (false, username, string.Empty);
  }

  // Cross-scene one-time error message for menu UI (for example join rejection).
  public static void SetPendingMenuError(string message)
  {
    _pendingMenuError = message ?? string.Empty;
  }

  public static string ConsumePendingMenuError()
  {
    var message = _pendingMenuError;
    _pendingMenuError = string.Empty;
    return message;
  }

  public static bool HasPendingMenuError()
  {
    return !string.IsNullOrWhiteSpace(_pendingMenuError);
  }

  // If user has no token, keep them on the gate scene (menu/login).
  private void EnforceLoginGate()
  {
    // Dedicated server should never be forced into the login gate.
    if (IsDesignatedServerMode())
    {
      return;
    }

    if (HasValidSession())
    {
      return;
    }

    var tree = GetTree();
    var currentScene = tree?.CurrentScene;
    if (currentScene == null)
    {
      return;
    }

    if (currentScene.SceneFilePath == GateScenePath)
    {
      return;
    }

    // Prevent repeated scene changes while one is already in flight.
    if (_isRedirectingToGate)
    {
      return;
    }

    _isRedirectingToGate = true;
    CallDeferred(MethodName.RedirectToGateScene);
  }

  private void RedirectToGateScene()
  {
    _isRedirectingToGate = false;

    // Re-check token right before redirect in case it changed this frame.
    if (HasValidSession())
    {
      return;
    }

    GetTree()?.ChangeSceneToFile(GateScenePath);
  }
}
