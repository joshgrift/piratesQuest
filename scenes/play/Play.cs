using Godot;
using System;

public partial class Play : Node3D
{
	[Export] private MultiplayerSpawner _playerSpawner;
	[Export] private MultiplayerSpawner _projectileSpawner;

	private PackedScene _playerScene = GD.Load<PackedScene>("res://scenes/player/player.tscn");
	private PackedScene _cannonBallScene = GD.Load<PackedScene>("res://scenes/cannon_ball/cannon_ball.tscn");

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		// Setup the spawner's spawn function to set authority correctly
		_playerSpawner.SpawnFunction = new Callable(this, MethodName.PlayerSpawnHandler);
		_projectileSpawner.SpawnFunction = new Callable(this, MethodName.ProjectileSpawnHandler);

		if (Multiplayer.IsServer())
		{
			GD.Print($"Server ready. Connected peers: {string.Join(", ", Multiplayer.GetPeers())}");

			// Spawn for the host
			SpawnPlayer(Multiplayer.GetUniqueId());

			// Spawn for any peers that are already connected
			foreach (var peerId in Multiplayer.GetPeers())
			{
				GD.Print($"Spawning for already-connected peer {peerId}");
				SpawnPlayer(peerId);
			}

			// Listen for new peers and spawn for them
			Multiplayer.PeerConnected += OnPeerConnected;
		}
		else
		{
			GD.Print($"Client ready. My peer ID: {Multiplayer.GetUniqueId()}");
		}
	}

	private CannonBall ProjectileSpawnHandler(Variant data)
	{
		var dict = data.AsGodotDictionary();

		Vector3 position = dict["position"].AsVector3();
		Vector3 direction = dict["direction"].AsVector3();
		float speed = dict["speed"].AsSingle();

		var ball = _cannonBallScene.Instantiate<CannonBall>();
		ball.Position = position;
		ball.Launch(direction, speed, "player");
		return ball;
	}

	private Player PlayerSpawnHandler(Variant data)
	{
		var peerId = data.AsInt32();
		var player = _playerScene.Instantiate<Player>();
		player.Name = $"Player{peerId}";
		player.Position = new Vector3(0, 2, 0);

		player.ProjectileSpawner = GetNode<MultiplayerSpawner>("Projectiles/ProjectileSpawner");

		// Set authority in the spawn callback (this is the right time!)
		player.SetMultiplayerAuthority(peerId);
		var sync = player.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		sync?.SetMultiplayerAuthority(peerId);

		return player;
	}

	private void OnPeerConnected(long peerId)
	{
		GD.Print($"Peer {peerId} connected, spawning their player");
		SpawnPlayer(peerId);
	}

	private void SpawnPlayer(long peerId)
	{
		_playerSpawner.Spawn(peerId);
		GD.Print($"Requested spawn for peer {peerId}");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			GetTree().ChangeSceneToFile("res://menu.tscn");
		}
	}
}
