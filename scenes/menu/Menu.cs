using Godot;
using System;
using Algonquin1;

public partial class Menu : Node2D
{
	[Export] public Container MultiplayerControls;

	private int _port = 8888;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;

		if (Configuration.IsDesignatedServerMode())
		{
			GD.Print($"Starting server on port {_port} due to --server flag");
			CallDeferred(MethodName.StartServer);

		}
		else
		{
			SetupMenuUI();
		}
	}

	private void SetupMenuUI()
	{
		var joinButton = MultiplayerControls.GetNodeOrNull<Button>("JoinButton");
		joinButton.ButtonDown += () =>
		{
			var ipBox = MultiplayerControls.GetNodeOrNull<LineEdit>("ServerIP");
			var ip = ipBox.Text.Trim();

			// Connect to server FIRST (NetworkManager is persistent)
			var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
			networkManager.CreateClient(ip, _port);

			// THEN change scene
			GetTree().ChangeSceneToFile("res://scenes/play/play.tscn");
		};
	}

	private void StartServer()
	{
		var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		networkManager.CreateServer(_port);
		GetTree().ChangeSceneToFile("res://scenes/play/play.tscn");
	}

	public override void _Input(InputEvent @event)
	{
		// A workaround way to toggle mouse mode for testing and/org getting the mouse stuck.
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.F)
		{
			GD.Print("Toggling mouse mode");
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
		}
	}
}
