using Godot;
using PiratesQuest;
using PiratesQuest.Data;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public partial class Menu : Node2D
{
  // We serialize menu state using camelCase so TypeScript receives idiomatic keys.
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  private Node _webView;
  private CanvasLayer _webViewLayer;
  private bool _webViewCreated;
  private bool _webViewReady;

  private bool _isAuthenticating;
  private bool _isAuthenticated;
  private string _username = string.Empty;
  private string _statusMessage = string.Empty;
  private string _statusTone = "info";
  private ServerListingInfo[] _servers = Array.Empty<ServerListingInfo>();

  public override void _Ready()
  {
    if (Configuration.IsDesignatedServerMode())
    {
      GD.Print($"Starting server on port {Configuration.DefaultPort} due to --server flag");
      CallDeferred(MethodName.StartServer);
      return;
    }

    EnsureWebViewCreated();
    InitializeMenuState();
  }

  private void InitializeMenuState()
  {
    _username = Configuration.GetUsername();
    _statusMessage = $"API: {Configuration.ApiBaseUrl}";
    _statusTone = "info";

    var gateState = Configuration.ResolveLoginGateState();
    if (!string.IsNullOrWhiteSpace(gateState.Username))
    {
      _username = gateState.Username;
    }

    if (!string.IsNullOrWhiteSpace(gateState.StatusMessage))
    {
      _statusMessage = gateState.StatusMessage;
      _statusTone = "info";
    }

    // CLI auth (used by run.sh --user/--password) should auto-login immediately.
    if (!string.IsNullOrWhiteSpace(Configuration.CmdUser) && !string.IsNullOrWhiteSpace(Configuration.CmdPassword))
    {
      _username = Configuration.CmdUser.Trim();
      _ = AttemptAuth("login", _username, Configuration.CmdPassword);
      return;
    }

    // If we already have a saved token, skip the login form.
    if (gateState.ShouldAutoLogin)
    {
      CompleteLogin();
      return;
    }

    PushMenuState();
  }

  // ── WebView setup ────────────────────────────────────────────────

  private void EnsureWebViewCreated()
  {
    if (_webViewCreated) return;
    _webViewCreated = true;

    if (!ClassDB.ClassExists("WebView"))
    {
      GD.PrintErr("Menu: WebView (godot_wry) plugin not available.");
      return;
    }

    var scene = GD.Load<PackedScene>("res://scenes/play/scenes/webview_node.tscn");
    if (scene == null)
    {
      GD.PrintErr("Menu: Failed to load webview scene.");
      return;
    }

    var instance = scene.Instantiate();
    if (instance is not Control control)
    {
      instance.QueueFree();
      GD.PrintErr("Menu: Webview scene root is not Control.");
      return;
    }

    _webView = control;
    _webView.Set("url", BuildMenuUrlWithCacheBuster());
    _webView.Set("full_window_size", false);
    _webView.Set("transparent", false);
    _webView.Set("devtools", true);
    _webView.Set("forward_input_events", true);
    _webView.Set("focused_when_created", true);

    _webViewLayer = new CanvasLayer();
    AddChild(_webViewLayer);
    _webViewLayer.AddChild(_webView);

    _webView.Connect("ipc_message", new Callable(this, MethodName.OnIpcMessage));
    GetTree().Root.SizeChanged += OnWindowResized;
    CallDeferred(MethodName.SyncWebViewSize);
  }

  private string BuildMenuUrlWithCacheBuster()
  {
    var menuUrl = Configuration.MenuWebViewUrl;
    var separator = menuUrl.Contains("?") ? "&" : "?";

    // Always include game version so new releases pull fresh menu assets.
    menuUrl = $"{menuUrl}{separator}v={Uri.EscapeDataString(Configuration.GetVersion())}";

    // In editor/debug, add a per-launch cache buster for rapid UI iteration.
    if (OS.HasFeature("editor") || OS.IsDebugBuild())
    {
      menuUrl = $"{menuUrl}&cb={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    return menuUrl;
  }

  private void OnWindowResized()
  {
    CallDeferred(MethodName.SyncWebViewSize);
  }

  private void SyncWebViewSize()
  {
    if (_webView is not Control wv) return;

    // Same mixed-DPI correction used by port webview.
    var viewportSize = GetTree().Root.Size;

    float maxScale = 1f;
    for (int i = 0; i < DisplayServer.GetScreenCount(); i++)
    {
      maxScale = Math.Max(maxScale, DisplayServer.ScreenGetScale(i));
    }

    float currentScale = DisplayServer.ScreenGetScale(DisplayServer.WindowGetCurrentScreen());
    float correction = currentScale / maxScale;

    wv.Position = Vector2.Zero;
    wv.Size = new Vector2(viewportSize.X * correction, viewportSize.Y * correction);
  }

  // ── IPC handling ──────────────────────────────────────────────────

  private void OnIpcMessage(string messageJson)
  {
    MenuIpcMessage message;
    try
    {
      message = JsonSerializer.Deserialize<MenuIpcMessage>(messageJson, JsonOptions);
    }
    catch (Exception ex)
    {
      GD.PrintErr($"Menu: Failed to parse IPC message: {ex.Message}");
      return;
    }

    switch (message)
    {
      case MenuReadyMessage:
        _webViewReady = true;
        PushMenuState();
        break;

      case MenuLoginMessage login:
        _ = AttemptAuth("login", login.Username, login.Password);
        break;

      case MenuSignupMessage signup:
        _ = AttemptAuth("signup", signup.Username, signup.Password);
        break;

      case MenuLogoutMessage:
        PerformLogout();
        break;

      case MenuRefreshServersMessage:
        _ = LoadServerListingsAsync();
        break;

      case MenuJoinServerMessage join:
        JoinServer(join.IpAddress, join.Port);
        break;

      case MenuSetBackgroundMutedMessage mute:
        SetBackgroundMuted(mute.Muted);
        break;

      case MenuOpenUrlMessage openUrl:
        OpenExternalUrl(openUrl.Url);
        break;
    }
  }

  // ── Auth & state ─────────────────────────────────────────────────

  private async Task AttemptAuth(string mode, string username, string password)
  {
    if (_isAuthenticating)
    {
      return;
    }

    username = username?.Trim() ?? string.Empty;
    password ??= string.Empty;

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
      SetError("Please enter username and password.");
      return;
    }

    _isAuthenticating = true;
    _statusMessage = mode == "signup" ? "Signing up..." : "Logging in...";
    _statusTone = "info";
    PushMenuState();

    var authResult = mode == "signup"
      ? await API.SignupAsync(username, password)
      : await API.LoginAsync(username, password);

    _isAuthenticating = false;

    if (!authResult.Success)
    {
      SetError(authResult.ErrorMessage);
      return;
    }

    var saveTokenError = Configuration.SaveUserToken(authResult.Token);
    if (saveTokenError != Error.Ok)
    {
      SetError($"Failed to save token: {saveTokenError}");
      return;
    }

    var saveUsernameError = Configuration.SaveUsername(username);
    if (saveUsernameError != Error.Ok)
    {
      SetError($"Failed to save username: {saveUsernameError}");
      return;
    }

    _username = username;
    CompleteLogin();
  }

  private void CompleteLogin()
  {
    _isAuthenticated = true;

    if (string.IsNullOrWhiteSpace(_username))
    {
      _username = Configuration.GetUsername().Trim();
    }

    ApplyUsernameToIdentity(_username);

    var pendingMenuError = Configuration.ConsumePendingMenuError();
    if (!string.IsNullOrWhiteSpace(pendingMenuError))
    {
      SetError(pendingMenuError);
      _ = LoadServerListingsAsync();
      return;
    }

    SetStatus("Authenticated.");
    _ = LoadServerListingsAsync();
  }

  private void PerformLogout()
  {
    var clearError = Configuration.ClearUserToken();
    if (clearError != Error.Ok)
    {
      SetError($"Failed to clear token: {clearError}");
      return;
    }

    _isAuthenticated = false;
    _isAuthenticating = false;
    _servers = Array.Empty<ServerListingInfo>();
    _username = string.Empty;

    ApplyUsernameToIdentity(string.Empty);
    SetStatus("Logged out. Please log in.");
  }

  private void ApplyUsernameToIdentity(string username)
  {
    var identity = GetNode<Identity>("/root/Identity");
    identity.PlayerName = username?.Trim() ?? string.Empty;
  }

  // ── Server list & join ───────────────────────────────────────────

  private async Task LoadServerListingsAsync()
  {
    if (!_isAuthenticated)
    {
      PushMenuState();
      return;
    }

    SetStatus("Loading servers...");

    var result = await API.GetServerListingsAsync();
    if (!result.Success)
    {
      if (result.IsUnauthorized)
      {
        PerformLogout();
        SetError(result.ErrorMessage);
        return;
      }

      SetError(result.ErrorMessage);
      return;
    }

    _servers = result.Servers;

    if (_servers.Length == 0)
    {
      SetStatus("No servers available right now.");
    }
    else
    {
      SetStatus("Servers loaded.");
    }
  }

  private void JoinServer(string ipAddress, int port)
  {
    var ip = ipAddress?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(ip))
    {
      SetError("Please enter a server address.");
      return;
    }

    SetStatus("Joining server...");

    var networkManager = GetNode<NetworkManager>("/root/NetworkManager");

    Multiplayer.ConnectedToServer += OnClientConnectedToServer;
    Multiplayer.ConnectionFailed += OnClientConnectionFailed;

    var error = networkManager.CreateClient(ip, port);
    if (error != Error.Ok)
    {
      Multiplayer.ConnectedToServer -= OnClientConnectedToServer;
      Multiplayer.ConnectionFailed -= OnClientConnectionFailed;
      SetError($"Failed to start connection: {error}");
    }
  }

  private void OnClientConnectedToServer()
  {
    Multiplayer.ConnectedToServer -= OnClientConnectedToServer;
    Multiplayer.ConnectionFailed -= OnClientConnectionFailed;
    GetTree().ChangeSceneToFile("res://scenes/play/play.tscn");
  }

  private void OnClientConnectionFailed()
  {
    Multiplayer.ConnectedToServer -= OnClientConnectedToServer;
    Multiplayer.ConnectionFailed -= OnClientConnectionFailed;
    SetError("Failed to connect to server.");
  }

  // ── Utility ──────────────────────────────────────────────────────

  private void SetBackgroundMuted(bool muted)
  {
    var audioManager = GetNode<AudioManager>("/root/AudioManager");
    audioManager.SetBackgroundMuted(muted);
    PushMenuState();
  }

  private void OpenExternalUrl(string url)
  {
    var trimmed = url?.Trim() ?? string.Empty;
    if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
    {
      SetError("Invalid URL.");
      return;
    }

    if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
    {
      SetError("Only http/https links are allowed.");
      return;
    }

    OS.ShellOpen(trimmed);
  }

  private void SetStatus(string message)
  {
    _statusMessage = message;
    _statusTone = "info";
    PushMenuState();
  }

  private void SetError(string message)
  {
    _statusMessage = message;
    _statusTone = "error";
    GD.PrintErr(message);
    PushMenuState();
  }

  private void PushMenuState()
  {
    if (!_webViewReady || _webView == null)
    {
      return;
    }

    var audioManager = GetNode<AudioManager>("/root/AudioManager");

    var state = new MenuStateDto
    {
      ApiBaseUrl = Configuration.ApiBaseUrl,
      Version = Configuration.GetVersion(),
      Username = _username,
      IsAuthenticated = _isAuthenticated,
      StatusMessage = _statusMessage,
      StatusTone = _statusTone,
      IsAuthenticating = _isAuthenticating,
      IsBackgroundMuted = audioManager.IsBackgroundMuted(),
      Servers = _servers.Select(server => new MenuServerListingDto
      {
        ServerName = server.ServerName,
        Description = server.Description,
        IpAddress = server.IpAddress,
        Port = server.Port,
        PlayerCount = server.PlayerCount,
        PlayerMax = server.PlayerMax,
        Status = server.Status,
        ServerVersion = server.ServerVersion,
      }).ToArray(),
    };

    var json = JsonSerializer.Serialize(state, JsonOptions);
    _webView.Call("eval", $"window.updateMenuState && window.updateMenuState({json})");
  }

  private void StartServer()
  {
    var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
    networkManager.CreateServer(Configuration.DefaultPort);
    GetTree().ChangeSceneToFile("res://scenes/play/play.tscn");
  }
}
