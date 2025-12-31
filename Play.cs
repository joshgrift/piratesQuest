using Godot;
using System;

public partial class Play : Node3D
{
	[Export] private MultiplayerSpawner _playerSpawner;
	private PackedScene _playerScene = GD.Load<PackedScene>("res://player.tscn");

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		// Setup the spawner's spawn function to set authority correctly
		_playerSpawner.SpawnFunction = new Callable(this, MethodName.CustomSpawn);

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

	// Custom spawn function called by MultiplayerSpawner
	private Node CustomSpawn(Variant data)
	{
		var peerId = data.AsInt32();
		var player = _playerScene.Instantiate<CharacterBody3D>();
		player.Name = $"Player{peerId}";
		player.Position = new Vector3(0, 2, 0);

		// Set authority in the spawn callback (this is the right time!)
		player.SetMultiplayerAuthority(peerId);
		var sync = player.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		if (sync != null)
		{
			sync.SetMultiplayerAuthority(peerId);
		}

		return player;
	}

	private void OnPeerConnected(long peerId)
	{
		GD.Print($"Peer {peerId} connected, spawning their player");
		SpawnPlayer(peerId);
	}

	private void SpawnPlayer(long peerId)
	{
		// Just tell the spawner to spawn with the peer ID as data
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
