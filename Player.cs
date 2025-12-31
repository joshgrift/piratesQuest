using Godot;

public partial class Player : CharacterBody3D
{
  // Don't forget to rebuild the project so the editor knows about the new export variable.

  // Maximum speed the ship can reach in meters per second.
  [Export]
  public float MaxSpeed { get; set; } = 14.0f;

  // How quickly the ship accelerates (smaller = more realistic/heavier ship)
  [Export]
  public float Acceleration { get; set; } = 3.0f;

  // How quickly the ship decelerates when no input (smaller = more drift/momentum)
  [Export]
  public float Deceleration { get; set; } = 1.5f;

  // The downward acceleration when in the air, in meters per second squared.
  [Export]
  public int FallAcceleration { get; set; } = 75;

  // Base turn rate when moving at max speed (radians per second)
  [Export]
  public float TurnSpeed { get; set; } = 1.5f;

  // Minimum speed needed before ship can turn effectively
  [Export]
  public float MinTurnSpeed { get; set; } = 2.0f;

  // Current forward speed of the ship (can be negative for reverse)
  private float _currentSpeed = 0.0f;

  private Vector3 _targetVelocity = Vector3.Zero;

  public override void _PhysicsProcess(double delta)
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
}
