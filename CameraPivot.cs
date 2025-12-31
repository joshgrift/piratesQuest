using Godot;
using System;

public partial class CameraPivot : Marker3D
{
	[Export]
	public float MouseSensitivity { get; set; } = 0.002f;

	private float _cameraTargetAngleY = 0.0f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Capture the mouse to lock it in the viewport and hide it
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			// Rotate around the Y axis (horizontal rotation only)
			_cameraTargetAngleY -= mouseMotion.Relative.X * MouseSensitivity;

			// Apply the rotation
			Rotation = new Vector3(Rotation.X, _cameraTargetAngleY, Rotation.Z);
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			GD.Print("Toggling mouse mode");
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
		}
	}

	// If the pivot needs to follow a moving target (like a player), update its position in _process or _physics_process
	// public override void _Process(double delta)
	// {
	//     GlobalPosition = GetParent<Node3D>().GlobalPosition;
	// }
}
