namespace PiratesQuest;

using System;
using System.Text.Json;
using Godot;
using PiratesQuest.Data;
using System.Collections.Generic;

public partial class Play : Node3D
{
  // 30 seconds keeps API traffic low while still giving regular "server is alive" updates.
  private const double HeartbeatIntervalSeconds = 60.0;

  [Export] private MultiplayerSpawner _playerSpawner;
  [Export] private MultiplayerSpawner _projectileSpawner;
  [Export] private MultiplayerSpawner _deadPlayerSpawner;
  [Export] private Node3D playerContainer;
  [Export] private FreeCam _freeCam;
  [Export] private Hud hud;

  private PackedScene _playerScene = GD.Load<PackedScene>("res://scenes/player/player.tscn");
  private PackedScene _cannonBallScene = GD.Load<PackedScene>("res://scenes/cannon_ball/cannon_ball.tscn");
  private PackedScene _deadPlayerScene = GD.Load<PackedScene>("res://scenes/dead_player/dead_player.tscn");
  private readonly Dictionary<long, string> _peerUsernames = new();
  private Timer _heartbeatTimer;

  public override void _Ready()
  {
    _playerSpawner.SpawnFunction = new Callable(this, MethodName.PlayerSpawnHandler);
    _projectileSpawner.SpawnFunction = new Callable(this, MethodName.ProjectileSpawnHandler);
    _deadPlayerSpawner.SpawnFunction = new Callable(this, MethodName.DeadPlayerSpawnHandler);

    // Start background ambient sounds
    // We use the AudioManager autoload singleton to play looping sounds
    // The "-10" volume (in decibels) makes these sounds quieter so they don't overpower gameplay
    StartBackgroundAudio();

    if (Multiplayer.IsServer())
    {
      GD.Print($"Server ready");
      Multiplayer.PeerConnected += OnPeerConnected;
      Multiplayer.PeerDisconnected += OnPeerDisconnected;

      var autoSaveTimer = new Timer { WaitTime = 30.0, Autostart = true };
      autoSaveTimer.Timeout += OnAutoSave;
      AddChild(autoSaveTimer);
      GD.Print("Auto-save timer started (every 30s)");

      StartServerHeartbeatIfNeeded();

      // Activate free camera in server mode
      if (Configuration.IsDesignatedServerMode() && _freeCam != null)
      {
        _freeCam.Current = true;
        GD.Print("Free camera activated for server mode");
      }
    }
    else
    {
      Multiplayer.ServerDisconnected += OnServerDisconnected;
      Multiplayer.ConnectionFailed += OnConnectionFailed;

      // Client must register username with server before the server spawns their player.
      RegisterLocalUsernameWithServer();
    }
  }

  private void OnPeerDisconnected(long peerId)
  {
    GD.Print($"Peer {peerId} disconnected, cleaning up their player");
    // Keep the username before removing it so we can report an offline event.
    var hadUsername = _peerUsernames.TryGetValue(peerId, out var username);
    _peerUsernames.Remove(peerId);

    if (hadUsername && !string.IsNullOrWhiteSpace(username))
    {
      NotifyPresence(username, false);
    }

    var playerNode = GetNodeOrNull<Player>($"SpawnPoint/player_{peerId}");
    if (playerNode != null)
    {
      SavePlayerState(playerNode);
      playerNode.QueueFree();
      GD.Print($"Removed player_{peerId} due to disconnect");
    }
  }

  private async void SavePlayerState(Player player)
  {
    if (string.IsNullOrEmpty(player.UserId))
    {
      GD.PrintErr($"{player.Name}: Cannot save state - no userId (auth handshake may not have completed)");
      return;
    }

    var json = player.LastSyncedStateJson;
    if (string.IsNullOrEmpty(json))
    {
      GD.PrintErr($"{player.Name}: No synced state available to save");
      return;
    }

    await ServerAPI.SavePlayerStateAsync(Configuration.ServerId, player.UserId, json);
  }

  /// <summary>
  /// Dedicated server only: send regular heartbeats to API so /api/status can show last seen UTC.
  /// </summary>
  private void StartServerHeartbeatIfNeeded()
  {
    // Local host/listen-server sessions don't have server API credentials.
    // Only dedicated server mode should send API heartbeats.
    if (!Configuration.IsDesignatedServerMode())
    {
      return;
    }

    _heartbeatTimer = new Timer
    {
      WaitTime = HeartbeatIntervalSeconds,
      Autostart = true
    };
    _heartbeatTimer.Timeout += OnHeartbeatTimerTimeout;
    AddChild(_heartbeatTimer);

    // Send an immediate heartbeat on startup so the API reflects "up" quickly.
    OnHeartbeatTimerTimeout();
    GD.Print($"Heartbeat timer started (every {HeartbeatIntervalSeconds:0.#}s)");
  }

  private void OnHeartbeatTimerTimeout()
  {
    _ = ServerAPI.SendHeartbeatAsync(Configuration.ServerId);
  }

  private void OnAutoSave()
  {
    var spawnPoint = GetNodeOrNull("SpawnPoint");
    if (spawnPoint == null) return;

    foreach (var child in spawnPoint.GetChildren())
    {
      if (child is Player player)
        SavePlayerState(player);
    }

    GD.Print("Auto-save completed for all connected players");
  }

  private CannonBall ProjectileSpawnHandler(Variant data)
  {
    var dict = data.AsGodotDictionary();

    Vector3 position = dict["position"].AsVector3();
    Vector3 direction = dict["direction"].AsVector3();
    float speed = dict["speed"].AsSingle();
    int damage = dict["damage"].AsInt32();
    string playerName = dict["playerName"].AsString();

    var ball = _cannonBallScene.Instantiate<CannonBall>();
    ball.Position = position;
    ball.Launch(direction, speed, playerName, damage);
    return ball;
  }

  private DeadPlayer DeadPlayerSpawnHandler(Variant data)
  {

    var dict = data.AsGodotDictionary();

    GD.Print($"Spawning DeadPlayer for {dict["nickname"].AsString()}");

    var deadPlayer = _deadPlayerScene.Instantiate<DeadPlayer>();
    deadPlayer.Position = dict["position"].AsVector3();
    deadPlayer.DroppedItems = dict["items"].AsGodotDictionary<InventoryItemType, int>();
    deadPlayer.Nickname = dict["nickname"].AsString();
    deadPlayer.PlayerName = dict["playerName"].AsString();

    return deadPlayer;
  }

  private Player PlayerSpawnHandler(Variant data)
  {
    var peerId = data.AsInt32();
    var player = _playerScene.Instantiate<Player>();
    var identity = GetNode<Identity>("/root/Identity");
    player.Name = $"player_{peerId}";
    player.Position = new Vector3(0, 2, 0);

    player.Death += playerName =>
    {
      CallDeferred(MethodName.HandleDeath, player);
    };

    player.ProjectileSpawner = GetNode<MultiplayerSpawner>("Projectiles/ProjectileSpawner");
    player.DeadPlayerSpawner = _deadPlayerSpawner;

    player.SetMultiplayerAuthority(peerId);
    var sync = player.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
    sync?.SetMultiplayerAuthority(peerId);

    return player;
  }

  private void HandleDeath(Player player)
  {
    // Hide the player while they wait to respawn
    player.Visible = false;

    GD.Print($"{player.Name} died - respawning in 5 seconds...");

    // After 5 seconds, respawn the player instead of returning to menu
    GetTree().CreateTimer(5.0f).Timeout += () =>
    {
      // Make player visible again
      player.Visible = true;

      // Call the respawn method to reset health and position
      player.Respawn();

      GD.Print($"{player.Name} has respawned!");
    };
  }

  private void OnPeerConnected(long peerId)
  {
    // Wait for this peer to send username via RPC before spawning.
    GD.Print($"Peer {peerId} connected. Waiting for username registration...");
  }

  private void SpawnPlayer(long peerId)
  {
    _playerSpawner.Spawn(peerId);
    GD.Print($"Requested spawn for peer {peerId}");
  }

  // Sent by each client after entering Play scene.
  // Server validates version + username and either spawns player or rejects join.
  [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
  private void RegisterUsername(string username, string clientVersion)
  {
    if (!Multiplayer.IsServer())
    {
      return;
    }

    var peerId = Multiplayer.GetRemoteSenderId();
    var normalizedUsername = (username ?? string.Empty).Trim();
    var normalizedClientVersion = NormalizeVersion(clientVersion);
    var normalizedServerVersion = NormalizeVersion(Configuration.GetVersion());

    if (string.IsNullOrWhiteSpace(normalizedUsername))
    {
      RejectPeer(peerId, "Username is required.");
      return;
    }

    // Version gate: only allow clients that match the server build.
    // This prevents protocol/state mismatches between different releases.
    if (!string.Equals(normalizedClientVersion, normalizedServerVersion, StringComparison.Ordinal))
    {
      var readableClientVersion = DisplayVersionForError(normalizedClientVersion);
      var readableServerVersion = DisplayVersionForError(normalizedServerVersion);
      RejectPeer(peerId, $"Version mismatch. Server is {readableServerVersion}, but your client is {readableClientVersion}. Please update and try again.");
      return;
    }

    // If this username is already connected, replace the old session with this one.
    // This lets the newest login win (useful if the player reconnects or logs in from another device).
    var existingPeerId = FindConnectedPeerIdByUsername(normalizedUsername, peerId);
    if (existingPeerId.HasValue)
    {
      var oldPeerId = existingPeerId.Value;
      GD.Print($"Username '{normalizedUsername}' is already connected on peer {oldPeerId}. Replacing with peer {peerId}.");
      NotifyPresence(normalizedUsername, false);

      // Remove the old username mapping now so the new peer can be registered immediately.
      // OnPeerDisconnected will run shortly after and clean up the old player node.
      _peerUsernames.Remove(oldPeerId);

      // Disconnect the old client so the new login can take over.
      // The old client will return to menu via OnServerDisconnected.
      if (IsPeerConnected(oldPeerId))
      {
        Multiplayer.MultiplayerPeer?.DisconnectPeer((int)oldPeerId, true);
      }
    }

    _peerUsernames[peerId] = normalizedUsername;
    SpawnPlayer(peerId);
    SetSpawnedPlayerNickname(peerId, normalizedUsername);
    NotifyPresence(normalizedUsername, true);
  }

  /// <summary>
  /// Fire-and-forget wrapper so network notifications never block gameplay.
  /// </summary>
  private void NotifyPresence(string username, bool isOnline)
  {
    _ = ServerAPI.NotifyPlayerPresenceAsync(Configuration.ServerId, username, isOnline);
  }

  /// <summary>
  /// Finds another connected peer using the same username (case-insensitive).
  /// Returns null if no duplicate is found.
  /// </summary>
  private long? FindConnectedPeerIdByUsername(string username, long peerIdToIgnore)
  {
    foreach (var entry in _peerUsernames)
    {
      if (entry.Key == peerIdToIgnore)
      {
        continue;
      }

      if (string.Equals(entry.Value, username, StringComparison.OrdinalIgnoreCase))
      {
        return entry.Key;
      }
    }

    return null;
  }

  private void RegisterLocalUsernameWithServer()
  {
    var identity = GetNode<Identity>("/root/Identity");
    var username = identity.PlayerName.Trim();
    var clientVersion = Configuration.GetVersion();

    // Send username + version in one handshake RPC so the server can validate
    // before spawning this player.
    RpcId(1, MethodName.RegisterUsername, username, clientVersion);
  }

  private static string NormalizeVersion(string version)
  {
    return (version ?? string.Empty).Trim();
  }

  private static string DisplayVersionForError(string version)
  {
    return string.IsNullOrWhiteSpace(version) ? "unknown" : version;
  }

  private void SetSpawnedPlayerNickname(long peerId, string username)
  {
    // Spawn happens through MultiplayerSpawner. Defer so node exists before lookup.
    CallDeferred(MethodName.SetSpawnedPlayerNicknameDeferred, peerId, username);
  }

  private void SetSpawnedPlayerNicknameDeferred(long peerId, string username)
  {
    var playerNode = GetNodeOrNull<Player>($"SpawnPoint/player_{peerId}");
    if (playerNode != null)
    {
      playerNode.Nickname = username;
    }
  }

  // Server calls this on the rejected client before disconnecting.
  [Rpc(MultiplayerApi.RpcMode.Authority)]
  private void OnJoinRejected(string reason)
  {
    GD.PrintErr($"Join rejected: {reason}");
    Configuration.SetPendingMenuError(reason);
    GetTree().ChangeSceneToFile("res://scenes/menu/menu.tscn");
  }

  private void DisconnectPeerAfterRejection(long peerId)
  {
    // Give the rejection RPC a moment to reach the client before disconnecting.
    GetTree().CreateTimer(0.1).Timeout += () =>
    {
      if (IsPeerConnected(peerId))
      {
        Multiplayer.MultiplayerPeer?.DisconnectPeer((int)peerId, true);
      }
    };
  }

  private void RejectPeer(long peerId, string reason)
  {
    if (!IsPeerConnected(peerId))
    {
      GD.PrintErr($"Cannot reject peer {peerId}: peer is no longer connected.");
      return;
    }

    RpcId(peerId, MethodName.OnJoinRejected, reason);
    DisconnectPeerAfterRejection(peerId);
  }

  private bool IsPeerConnected(long peerId)
  {
    return Array.IndexOf(Multiplayer.GetPeers(), (int)peerId) >= 0;
  }

  private void OnServerDisconnected()
  {
    // Safety net: if rejection RPC is missed, still return to menu.
    if (!Configuration.HasPendingMenuError())
    {
      Configuration.SetPendingMenuError("Disconnected from server.");
    }
    GetTree().ChangeSceneToFile("res://scenes/menu/menu.tscn");
  }

  private void OnConnectionFailed()
  {
    Configuration.SetPendingMenuError("Connection to server failed.");
    GetTree().ChangeSceneToFile("res://scenes/menu/menu.tscn");
  }

  /// <summary>
  /// Starts the ambient background sounds for the Play scene.
  /// Uses the AudioManager singleton to play looping audio.
  ///
  /// Key concepts:
  /// - GetNode<T>("/root/Name") gets an autoload (singleton) node
  /// - PlayLoop takes: a unique name (to stop it later), the sound path, and volume in dB
  /// - Negative dB = quieter, 0 = normal, positive = louder
  /// </summary>
  private void StartBackgroundAudio()
  {
    // Get the AudioManager autoload singleton
    // Autoloads are global nodes added in Project Settings > AutoLoad
    // They're always available at /root/[Name]
    var audioManager = GetNode<AudioManager>("/root/AudioManager");

    // Play ocean waves - this is the main ambient sound
    // Volume is set to -15 dB (quieter than normal) so it doesn't overpower other sounds
    audioManager.PlayLoop(
      "ocean",                               // Unique identifier - use this name to stop it later
      "res://art/sounds/sea.wav",            // Path to the sound file
      -15.0f                                 // Volume in decibels (negative = quieter)
    );

    // Note: Creaking sounds are handled by each Player when they move
    // This makes more sense - the ship creaks when YOU sail, not constantly!

    GD.Print("Background audio started: ocean waves");
  }

  /// <summary>
  /// Called when this node is about to be removed from the scene tree.
  /// We use this to clean up our audio loops.
  ///
  /// _ExitTree is the opposite of _Ready - good for cleanup!
  /// </summary>
  public override void _ExitTree()
  {
    if (!Multiplayer.IsServer())
    {
      Multiplayer.ServerDisconnected -= OnServerDisconnected;
      Multiplayer.ConnectionFailed -= OnConnectionFailed;
    }

    // Stop our background audio when leaving this scene
    // This prevents sounds from continuing to play after leaving
    var audioManager = GetNodeOrNull<AudioManager>("/root/AudioManager");

    // GetNodeOrNull returns null if the node doesn't exist (safer than GetNode)
    // This can happen if the game is shutting down
    if (audioManager != null)
    {
      audioManager.StopLoop("ocean");
      GD.Print("Background audio stopped");
    }

    if (_heartbeatTimer != null)
    {
      _heartbeatTimer.Timeout -= OnHeartbeatTimerTimeout;
    }
  }
}
