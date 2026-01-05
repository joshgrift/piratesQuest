using Algonquin1;
using Algonquin1.Attributes;
using Godot;
using Godot.Collections;

public partial class Player : CharacterBody3D, ICanCollect, IDamageable
{
	[Signal] public delegate void InventoryChangedEventHandler(InventoryItemType itemType, int newAmount);
	[Signal] public delegate void CannonFiredEventHandler();
	[Signal] public delegate void CannonReadyToFireEventHandler();
	[Signal] public delegate void DeathEventHandler(string playerName);
	[Signal] public delegate void HealthUpdateEventHandler(int newHealth);

	[Export] public float MaxSpeed { get; set; } = 14.0f;
	[Export] public float Acceleration { get; set; } = 8.0f;
	[Export] public float Deceleration { get; set; } = 4.0f;
	[Export] public int FallAcceleration { get; set; } = 75;
	[Export] public float TurnSpeed { get; set; } = 0.5f;
	[Export] public float MinTurnSpeed { get; set; } = 0.0f;

	[Export] public int Health { get; set; } = 100;
	[Export] public int MaxHealth { get; set; } = 100;

	[Export] public Node3D CannonPivot;
	[Export] public MultiplayerSpawner ProjectileSpawner;

	private float _currentSpeed = 0.0f;
	private Vector3 _targetVelocity = Vector3.Zero;
	private readonly Inventory _inventory = new();
	private int _fireCoolDownInSeconds = 2;
	private double _firedTimerCountdown = 0;

	public override void _Ready()
	{
		// Only enable the camera for the player we control
		var camera = GetNodeOrNull<Camera3D>("CameraPivot/Camera3D");
		if (camera != null)
		{
			camera.Current = IsMultiplayerAuthority();
			GD.Print($"{Name}: Camera enabled = {camera.Current}");
		}

		const int startYRange = 100;
		const int startXRange = 100;

		// Randomize starting position	
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		GlobalPosition = new Vector3(
			rng.Randf() * startYRange - startYRange / 2,
			GlobalPosition.Y,
			rng.Randf() * startXRange - startXRange / 2
		);

		CallDeferred(MethodName.UpdateInventory, (int)InventoryItemType.Ammo, 10);
		CallDeferred(MethodName.UpdateInventory, (int)InventoryItemType.G, 10000);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsMultiplayerAuthority())
		{
			// Movement
			UpdateMovement((float)delta);

			// Firing
			if (_firedTimerCountdown < 0)
			{
				_firedTimerCountdown = 0;
				EmitSignal(SignalName.CannonReadyToFire);
			}
			else
			{
				_firedTimerCountdown -= delta;
			}

			if (Input.IsActionPressed("fire_cannons") && _firedTimerCountdown <= 0)
			{
				FireCannons();
			}
		}
	}

	private void FireCannons()
	{
		if (_inventory.GetItemCount(InventoryItemType.Ammo) <= 0)
		{
			GD.PrintErr($"{Name} tried to fire cannons but has no cannonballs!");
			_firedTimerCountdown = 0.1f;
			return;
		}

		UpdateInventory(InventoryItemType.Ammo, -1);

		_firedTimerCountdown = _fireCoolDownInSeconds;
		var spawnData = new Godot.Collections.Dictionary
		{
			["position"] = CannonPivot.GlobalPosition,
			["direction"] = GlobalTransform.Basis.Z * -1,
			["speed"] = _currentSpeed,
			["playerName"] = Name
		};

		Rpc(MethodName.RequestFireCannons, spawnData);
		EmitSignal(SignalName.CannonFired);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RequestFireCannons(Variant data)
	{
		if (!Multiplayer.IsServer()) return;

		ProjectileSpawner.Spawn(data);
	}

	private void UpdateMovement(float delta)
	{
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

		// Allow turning with responsive controls (no speed requirement)
		if (turnInput != 0.0f)
		{
			// Rotate the ship - simple and responsive
			RotateY(-turnInput * TurnSpeed * (float)delta);
		}

		// Move the ship in the direction it's facing
		// The pivot's forward direction is -Z in local space
		Vector3 forwardDirection = -GlobalTransform.Basis.Z;

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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void TakeDamage(int amount)
	{
		if (!IsMultiplayerAuthority()) return;

		Health -= amount;
		if (Health <= 0)
		{
			OnDeath();
		}
		else
		{
			EmitSignal(SignalName.HealthUpdate, Health);
		}
		GD.Print($"health = {Health}");
	}

	public void OnDeath()
	{
		GD.Print($"dead: {Name}");
		EmitSignal(SignalName.Death, Name);
	}

	public bool UpdateInventory(InventoryItemType item, int amount)
	{
		return UpdateInventory(item, amount, 0);
	}

	public bool UpdateInventory(InventoryItemType item, int amount, int price = 0)
	{
		GD.Print($"{Name} updating inventory: {item} by {amount} (price: {price})");

		if (amount < 0 && _inventory.GetItemCount(item) < -amount)
		{
			GD.PrintErr($"Not enough {item} to remove. Current: {_inventory.GetItemCount(item)}, Tried to remove: {-amount}");
			return false;
		}

		if (_inventory.GetItemCount(InventoryItemType.G) + price < 0)
		{
			GD.PrintErr($"Cannot afford transaction. Current Gold: {_inventory.GetItemCount(InventoryItemType.G)}, Price: {price}");
			return false;
		}

		_inventory.AddItem(item, amount);
		if (price != 0)
		{
			_inventory.AddItem(InventoryItemType.G, price);
			EmitSignal(SignalName.InventoryChanged, (int)InventoryItemType.G, _inventory.GetItemCount(InventoryItemType.G));
		}

		EmitSignal(SignalName.InventoryChanged, (int)item, _inventory.GetItemCount(item));
		return true;
	}

	public Dictionary<InventoryItemType, int> GetInventory()
	{
		return _inventory.GetAll();
	}
}
