namespace Algonquin1;

using Godot;

public partial class Play : Node3D
{
	[Export] private MultiplayerSpawner _playerSpawner;
	[Export] private MultiplayerSpawner _projectileSpawner;

	private PackedScene _playerScene = GD.Load<PackedScene>("res://scenes/player/player.tscn");
	private PackedScene _cannonBallScene = GD.Load<PackedScene>("res://scenes/cannon_ball/cannon_ball.tscn");

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_playerSpawner.SpawnFunction = new Callable(this, MethodName.PlayerSpawnHandler);
		_projectileSpawner.SpawnFunction = new Callable(this, MethodName.ProjectileSpawnHandler);

		if (Multiplayer.IsServer())
		{
			GD.Print($"Server ready");
			Multiplayer.PeerConnected += OnPeerConnected;
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

	private Player PlayerSpawnHandler(Variant data)
	{
		var peerId = data.AsInt32();
		var player = _playerScene.Instantiate<Player>();
		player.Name = $"player_{peerId}";
		player.Position = new Vector3(0, 2, 0);

		player.Death += playerName =>
		{
			CallDeferred(MethodName.HandleDeath, playerName);
		};

		player.ProjectileSpawner = GetNode<MultiplayerSpawner>("Projectiles/ProjectileSpawner");

		// Set authority in the spawn callback (this is the right time!)
		player.SetMultiplayerAuthority(peerId);
		var sync = player.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		sync?.SetMultiplayerAuthority(peerId);

		return player;
	}

	private void HandleDeath(string playerName)
	{
		Rpc(MethodName.DespawnPlayer, playerName);
		GD.Print($"{playerName} died, returning to menu");
		GetTree().ChangeSceneToFile("res://scenes/menu/menu.tscn");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void DespawnPlayer(string playerName)
	{
		if (!Multiplayer.IsServer()) return;

		var playerNode = GetNodeOrNull<Player>($"SpawnPoint/{playerName}");
		if (playerNode != null)
		{
			playerNode.CallDeferred(Player.MethodName.QueueFree);
			GD.Print($"Despawned player {playerName}");
		}
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
