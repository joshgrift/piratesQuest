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
    string playerName = dict["playerName"].AsString();

    var ball = _cannonBallScene.Instantiate<CannonBall>();
    ball.Position = position;
    ball.Launch(direction, speed, playerName);
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
    player.Visible = false;

    GetTree().CreateTimer(4.0f).Timeout += () =>
    {
      Multiplayer.MultiplayerPeer.Close();
      GD.Print("Disconnected from multiplayer, returning to menu");
      GetTree().ChangeSceneToFile("res://scenes/menu/menu.tscn");
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
}
