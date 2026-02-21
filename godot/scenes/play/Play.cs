namespace PiratesQuest;

using System;
using Godot;
using PiratesQuest.Data;

public partial class Play : Node3D
{
  [Export] private MultiplayerSpawner _playerSpawner;
  [Export] private MultiplayerSpawner _projectileSpawner;
  [Export] private MultiplayerSpawner _deadPlayerSpawner;
  [Export] private Node3D playerContainer;
  [Export] private FreeCam _freeCam;
  [Export] private Hud hud;

  private PackedScene _playerScene = GD.Load<PackedScene>("res://scenes/player/player.tscn");
  private PackedScene _cannonBallScene = GD.Load<PackedScene>("res://scenes/cannon_ball/cannon_ball.tscn");
  private PackedScene _deadPlayerScene = GD.Load<PackedScene>("res://scenes/dead_player/dead_player.tscn");

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

      // Activate free camera in server mode
      if (Configuration.IsDesignatedServerMode() && _freeCam != null)
      {
        _freeCam.Current = true;
        GD.Print("Free camera activated for server mode");
      }
    }
  }

  private void OnPeerDisconnected(long peerId)
  {
    GD.Print($"Peer {peerId} disconnected, cleaning up their player");

    // Clean up the disconnected player's node
    var playerNode = GetNodeOrNull<Player>($"SpawnPoint/player_{peerId}");
    if (playerNode != null)
    {
      playerNode.QueueFree();
      GD.Print($"Removed player_{peerId} due to disconnect");
    }
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

  private async void OnPeerConnected(long peerId)
  {
    GD.Print($"Spawning player for peer {peerId}");
    SpawnPlayer(peerId);
  }

  private void SpawnPlayer(long peerId)
  {
    _playerSpawner.Spawn(peerId);
    GD.Print($"Requested spawn for peer {peerId}");
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
  }
}
