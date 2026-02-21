using Godot;
using System;
using PiratesQuest;

public partial class Menu : Node2D
{
  [Export] public Container MultiplayerControls;
  [Export] public Container ServerListingsContainer;
  [Export] public Container PlayerIdentityContainer;
  [Export] public Label StatusLabel;
  [Export] public Button MuteButton;

  private PackedScene _listingScene = GD.Load<PackedScene>("res://scenes/menu/scenes/server_listing.tscn");

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
    }
  }

  private void SetupMenuUI()
  {
    // Player Identity Handlers
    PlayerIdentityContainer.GetNode<LineEdit>("PlayerNameEdit").TextChanged += (newText) =>
    {
      var identity = GetNode<Identity>("/root/Identity");
      identity.PlayerName = newText;
    };

    // Connect to Server Listings
    foreach (var serverListing in Configuration.GetDefaultServerListings())
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
