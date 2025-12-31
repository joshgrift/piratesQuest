using Algonquin1;
using Godot;

public partial class Player : CharacterBody3D, ICollector
{
	[Signal]
	public delegate void InventoryChangedEventHandler(InventoryItemType itemType, int newAmount);

	[Export]
	public float MaxSpeed { get; set; } = 14.0f;

	[Export]
	public float Acceleration { get; set; } = 3.0f;

	[Export]
	public float Deceleration { get; set; } = 1.5f;

	[Export]
	public int FallAcceleration { get; set; } = 75;

	[Export]
	public float TurnSpeed { get; set; } = 1.5f;

	[Export]
	public float MinTurnSpeed { get; set; } = 2.0f;

	private float _currentSpeed = 0.0f;

	private Vector3 _targetVelocity = Vector3.Zero;

	private readonly Inventory _inventory = new();

	public override void _Ready()
	{
		// Only enable the camera for the player we control
		var camera = GetNodeOrNull<Camera3D>("CameraPivot/Camera3D");
		if (camera != null)
		{
			camera.Current = IsMultiplayerAuthority();
			GD.Print($"{Name}: Camera enabled = {camera.Current}");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsMultiplayerAuthority())
		{
			UpdateMovement((float)delta);
		}
	}

	private void UpdateMovement(float delta)
	{
		var pivot = GetNode<Node3D>("Pivot");

		// Get input for forward/backward movement
		float forwardInput = 0.0f;
		if (Input.IsActionPressed("move_forward"))
		{
			forwardInput = 1.0f;
		}
		if (Input.IsActionPressed("move_back"))
		{
			forwardInput = -1.0f;
		}

		// Get input for turning (left/right)
		float turnInput = 0.0f;
		if (Input.IsActionPressed("move_right"))
		{
			turnInput = 1.0f;
		}
		if (Input.IsActionPressed("move_left"))
		{
			turnInput = -1.0f;
		}

		// Update speed based on forward input
		if (forwardInput != 0.0f)
		{
			// Accelerate in the direction of input
			float targetSpeed = forwardInput * MaxSpeed;
			_currentSpeed = Mathf.MoveToward(_currentSpeed, targetSpeed, Acceleration * (float)delta);
		}
		else
		{
			// Decelerate when no input (ship drifts to a stop)
			_currentSpeed = Mathf.MoveToward(_currentSpeed, 0.0f, Deceleration * (float)delta);
		}

		// Only allow turning when moving (ships need water flow over rudder to turn)
		if (Mathf.Abs(_currentSpeed) > MinTurnSpeed && turnInput != 0.0f)
		{
			// Turn rate increases with speed (faster ships turn better)
			// Also, turning is reversed when going backwards (like a real ship)
			float speedFactor = Mathf.Abs(_currentSpeed) / MaxSpeed;
			float effectiveTurnSpeed = TurnSpeed * speedFactor;

			// When reversing, turning is inverted (like steering a car in reverse)
			float turnDirection = _currentSpeed > 0 ? turnInput : -turnInput;

			// Rotate the pivot (and thus the ship model)
			pivot.RotateY(-turnDirection * effectiveTurnSpeed * (float)delta);
		}

		// Move the ship in the direction it's facing
		// The pivot's forward direction is -Z in local space
		Vector3 forwardDirection = -pivot.GlobalTransform.Basis.Z;

		// Ground velocity - ship only moves forward/backward in facing direction
		_targetVelocity.X = forwardDirection.X * _currentSpeed;
		_targetVelocity.Z = forwardDirection.Z * _currentSpeed;

		// Vertical velocity
		if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			_targetVelocity.Y -= FallAcceleration * (float)delta;
		}

		// Moving the character
		Velocity = _targetVelocity;
		MoveAndSlide();
	}

	public bool Collect(InventoryItemType item, int amount)
	{
		_inventory.AddItem(item, amount);
		EmitSignal(SignalName.InventoryChanged, (int)item, _inventory.GetItemCount(item));
		return true;
	}
}
