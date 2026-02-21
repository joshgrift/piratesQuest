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
  // Godot's per-user, writable save file. On each OS this maps to a safe local app-data folder.
  private const string LocalConfigPath = "user://settings.cfg";
  private const string AuthSection = "auth";
  private const string UserTokenKey = "user_token";
  private const string UsernameKey = "username";
  private const string GateScenePath = "res://scenes/menu/menu.tscn";

  private bool _isRedirectingToGate = false;

  public static bool RandomSpawnEnabled { get; } = true;
  public static int StartingCoin { get; } = 100;
  public static bool IsCreative { get; } = false;
  public static int DefaultPort { get; } = 7777;
  public static string ApiBaseUrl { get; } = "http://localhost:5236";

  public override void _Ready()
  {
    CallDeferred(MethodName.ConfigureWindowTitle);
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

  // Saves the current auth token locally.
  // Returns Godot Error.Ok on success.
  public static Error SaveUserToken(string userToken)
  {
    var config = new ConfigFile();

    // Load first so we keep existing values in this file.
    var loadError = config.Load(LocalConfigPath);
    if (loadError != Error.Ok && loadError != Error.FileNotFound)
    {
      return loadError;
    }

    config.SetValue(AuthSection, UserTokenKey, userToken ?? string.Empty);
    return config.Save(LocalConfigPath);
  }

  // Reads the saved auth token from local storage.
  // Returns empty string if the file/key is missing.
  public static string GetUserToken()
  {
    var config = new ConfigFile();
    var loadError = config.Load(LocalConfigPath);
    if (loadError != Error.Ok)
    {
      return string.Empty;
    }

    return config.GetValue(AuthSection, UserTokenKey, string.Empty).AsString();
  }

  // Convenience helper to log out / remove auth in local storage.
  public static Error ClearUserToken()
  {
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
