using Godot;
using System;
using System.Threading.Tasks;
using PiratesQuest;

public partial class Menu : Node2D
{
  [Export] public Control MainMenuContainer;
  [Export] public Control LoginContainer;
  [Export] public LineEdit UsernameEdit;
  [Export] public LineEdit PasswordEdit;
  [Export] public Button LoginButton;
  [Export] public Button SignupButton;
  [Export] public Label LoginStatusLabel;

  [Export] public Container MultiplayerControls;
  [Export] public Container ServerListingsContainer;
  [Export] public Container PlayerIdentityContainer;
  [Export] public Label StatusLabel;
  [Export] public Button MuteButton;
  [Export] public Button LogoutButton;

  private PackedScene _listingScene = GD.Load<PackedScene>("res://scenes/menu/scenes/server_listing.tscn");
  private bool _isAuthenticating = false;

  public override void _Ready()
  {
    if (Configuration.IsDesignatedServerMode())
    {
      GD.Print($"Starting server on port {Configuration.DefaultPort} due to --server flag");
      CallDeferred(MethodName.StartServer);
    }
    else
    {
      SetupMenuUI();
      SetupLoginUI();
    }
  }

  private void SetupLoginUI()
  {
    // Require login before showing any of the existing menu controls.
    SetMainMenuVisible(false);
    LoginContainer.Visible = true;

    var savedToken = Configuration.GetUserToken();
    var savedUsername = Configuration.GetUsername();
    UsernameEdit.Text = savedUsername;
    LoginStatusLabel.Text = $"API: {Configuration.ApiBaseUrl}";

    LoginButton.Pressed += () => _ = AttemptAuth("login");
    SignupButton.Pressed += () => _ = AttemptAuth("signup");
    PasswordEdit.TextSubmitted += (_submittedText) =>
    {
      _ = AttemptAuth("login");
    };

    // If token + username are saved, log in immediately.
    if (!string.IsNullOrWhiteSpace(savedToken) && !string.IsNullOrWhiteSpace(savedUsername))
    {
      CompleteLogin();
      return;
    }

    // Old local data may contain only a token. Clear it so the user can re-login cleanly.
    if (!string.IsNullOrWhiteSpace(savedToken) && string.IsNullOrWhiteSpace(savedUsername))
    {
      _ = Configuration.ClearUserToken();
      LoginStatusLabel.Text = "Please log in again.";
      LoginStatusLabel.AddThemeColorOverride("font_color", Colors.White);
    }

    UsernameEdit.GrabFocus();
  }

  private void SetupMenuUI()
  {
    // Player name comes from authenticated username, so this field is display-only.
    var playerNameEdit = PlayerIdentityContainer.GetNode<LineEdit>("PlayerNameEdit");
    playerNameEdit.Editable = false;

    // Custom join
    var joinButton = MultiplayerControls.GetNodeOrNull<Button>("JoinButton");
    joinButton.ButtonDown += () =>
    {
      var ipBox = MultiplayerControls.GetNodeOrNull<LineEdit>("ServerIP");
      var ip = ipBox.Text.Trim();
      JoinServer(ip, Configuration.DefaultPort);
    };

    // Version Label
    var versionLabel = GetNodeOrNull<Label>("CanvasLayer/VersionLabel");
    if (versionLabel != null)
    {
      versionLabel.Text = Configuration.GetVersion();
    }

    // Mute Button - toggles background sounds (ocean waves) on/off
    // Other sounds like cannon fire still play - only ambient loops are affected
    var muteButton = MuteButton;
    var audioManager = GetNode<AudioManager>("/root/AudioManager");

    // Set initial button state to match current mute status
    // This ensures the button shows the correct state if we return to menu
    muteButton.ButtonPressed = audioManager.IsBackgroundMuted();
    muteButton.Text = audioManager.IsBackgroundMuted() ? "Unmute Sea" : "Mute Sea";

    // Toggled signal fires when a toggle button changes state
    // The 'toggled' parameter tells us if the button is now pressed (true) or not (false)
    muteButton.Toggled += (toggled) =>
    {
      audioManager.SetBackgroundMuted(toggled);
      // Update button text to show what will happen when clicked
      muteButton.Text = toggled ? "Unmute Sea" : "Mute Sea";
    };

    // Clear saved token and return to the login gate.
    LogoutButton.Pressed += PerformLogout;
  }

  private async Task AttemptAuth(string mode)
  {
    if (_isAuthenticating)
    {
      return;
    }

    var username = UsernameEdit.Text.Trim();
    var password = PasswordEdit.Text;
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
      LoginStatusLabel.Text = "Please enter username and password.";
      LoginStatusLabel.AddThemeColorOverride("font_color", Colors.Red);
      return;
    }

    _isAuthenticating = true;
    LoginButton.Disabled = true;
    SignupButton.Disabled = true;
    LoginStatusLabel.Text = mode == "signup" ? "Signing up..." : "Logging in...";
    LoginStatusLabel.AddThemeColorOverride("font_color", Colors.White);

    var authResult = mode == "signup"
      ? await API.SignupAsync(username, password)
      : await API.LoginAsync(username, password);

    _isAuthenticating = false;
    LoginButton.Disabled = false;
    SignupButton.Disabled = false;

    if (!authResult.Success)
    {
      LoginStatusLabel.Text = authResult.ErrorMessage;
      LoginStatusLabel.AddThemeColorOverride("font_color", Colors.Red);
      return;
    }

    var saveError = Configuration.SaveUserToken(authResult.Token);
    if (saveError != Error.Ok)
    {
      LoginStatusLabel.Text = $"Failed to save token: {saveError}";
      LoginStatusLabel.AddThemeColorOverride("font_color", Colors.Red);
      return;
    }

    var saveUsernameError = Configuration.SaveUsername(username);
    if (saveUsernameError != Error.Ok)
    {
      LoginStatusLabel.Text = $"Failed to save username: {saveUsernameError}";
      LoginStatusLabel.AddThemeColorOverride("font_color", Colors.Red);
      return;
    }

    CompleteLogin();
  }

  private void CompleteLogin()
  {
    ApplyUsernameToIdentity(Configuration.GetUsername());
    LoginContainer.Visible = false;
    SetMainMenuVisible(true);
    UsernameEdit.ReleaseFocus();
    PasswordEdit.ReleaseFocus();
    DisplayStatus("Authenticated.");
    _ = LoadServerListingsAsync();
  }

  private void SetMainMenuVisible(bool isVisible)
  {
    MainMenuContainer.Visible = isVisible;
  }

  private void PerformLogout()
  {
    var clearError = Configuration.ClearUserToken();
    if (clearError != Error.Ok)
    {
      DisplayError($"Failed to clear token: {clearError}");
      return;
    }

    UsernameEdit.Text = string.Empty;
    PasswordEdit.Text = string.Empty;
    ApplyUsernameToIdentity(string.Empty);
    LoginStatusLabel.Text = "Logged out. Please log in.";
    LoginStatusLabel.AddThemeColorOverride("font_color", Colors.White);
    LoginContainer.Visible = true;
    SetMainMenuVisible(false);
    UsernameEdit.GrabFocus();
  }

  private void ApplyUsernameToIdentity(string username)
  {
    var safeUsername = username?.Trim() ?? string.Empty;
    var identity = GetNode<Identity>("/root/Identity");
    identity.PlayerName = safeUsername;

    var playerNameEdit = PlayerIdentityContainer.GetNode<LineEdit>("PlayerNameEdit");
    playerNameEdit.Text = safeUsername;
  }

  private async Task LoadServerListingsAsync()
  {
    // Remove existing rows so re-login doesn't duplicate listing entries.
    foreach (Node child in ServerListingsContainer.GetChildren())
    {
      child.QueueFree();
    }

    DisplayStatus("Loading servers...");
    var result = await API.GetServerListingsAsync();
    if (!result.Success)
    {
      DisplayError(result.ErrorMessage);
      return;
    }

    foreach (var server in result.Servers)
    {
      AddServerListing(server);
    }

    if (result.Servers.Length == 0)
    {
      DisplayStatus("No servers available right now.");
    }
    else
    {
      DisplayStatus("Servers loaded.");
    }
  }

  private void AddServerListing(ServerListingInfo serverListing)
  {
    var listingInstance = _listingScene.Instantiate<ServerListing>();
    listingInstance.ServerName = serverListing.ServerName;
    listingInstance.IpAddress = serverListing.IpAddress;
    listingInstance.Port = serverListing.Port;
    listingInstance.PlayerCount = "x";
    listingInstance.PlayerMax = "8";

    ServerListingsContainer.AddChild(listingInstance);

    listingInstance.JoinServer += (ip, port) =>
    {
      JoinServer(ip, port);
    };
  }

  private void JoinServer(string ipAddress, int port)
  {
    DisplayStatus($"Joining server...");
    var networkManager = GetNode<NetworkManager>("/root/NetworkManager");

    Multiplayer.ConnectedToServer += OnClientConnectedToServer;
    Multiplayer.ConnectionFailed += OnClientConnectionFailed;
    var error = networkManager.CreateClient(ipAddress, port);

    if (error != Error.Ok)
    {
      Multiplayer.ConnectedToServer -= OnClientConnectedToServer;
      Multiplayer.ConnectionFailed -= OnClientConnectionFailed;

      DisplayError($"Failed to start connection: {error}");
    }
  }

  private void OnClientConnectedToServer()
  {
    GD.Print("Client connected successfully, changing scene...");
    Multiplayer.ConnectedToServer -= OnClientConnectedToServer;
    Multiplayer.ConnectionFailed -= OnClientConnectionFailed;
    GetTree().ChangeSceneToFile("res://scenes/play/play.tscn");
  }

  private void OnClientConnectionFailed()
  {
    DisplayError("Failed to connect to sever");
    Multiplayer.ConnectedToServer -= OnClientConnectedToServer;
    Multiplayer.ConnectionFailed -= OnClientConnectionFailed;
  }

  private void DisplayError(string errorMessage)
  {
    StatusLabel.Text = errorMessage;
    StatusLabel.AddThemeColorOverride("font_color", Colors.Red);
    GD.PrintErr(errorMessage);

    var timer = GetTree().CreateTimer(3.0);
    timer.Timeout += () =>
    {
      if (StatusLabel != null)
      {
        StatusLabel.Text = "";
      }
    };
  }

  private void DisplayStatus(string statusMessage)
  {
    GD.Print(statusMessage);
    StatusLabel.Text = statusMessage;
    StatusLabel.AddThemeColorOverride("font_color", Colors.White);
  }

  private void StartServer()
  {
    var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
    networkManager.CreateServer(Configuration.DefaultPort);
    GetTree().ChangeSceneToFile("res://scenes/play/play.tscn");
  }
}
