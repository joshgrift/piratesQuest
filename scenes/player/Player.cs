namespace PiratesQuest;

using PiratesQuest.Attributes;
using Godot;
using Godot.Collections;
using PiratesQuest.Data;
using System;

public class OwnedComponent
{
	public Component Component;
	public bool isEquipped;
}

public partial class Player : CharacterBody3D, ICanCollect, IDamageable
{
	[Signal] public delegate void InventoryChangedEventHandler(InventoryItemType itemType, int newAmount);
	[Signal] public delegate void CannonFiredEventHandler();
	[Signal] public delegate void CannonReadyToFireEventHandler();
	[Signal] public delegate void DeathEventHandler(string playerName);
	[Signal] public delegate void HealthUpdateEventHandler(int newHealth);

	public readonly PlayerStats Stats = new();
	public System.Collections.Generic.List<OwnedComponent> OwnedComponents = [];

	public int FallAcceleration { get; set; } = 75;
	public int Health { get; set; } = 100;
	public int MaxHealth => (int)Stats.GetStat(PlayerStat.ShipHullStrength);

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

		if (Configuration.RandomSpawnEnabled)
			RandomSpawn(100, 100);

		if (Configuration.IsCreative)
		{
			foreach (InventoryItemType itemType in Enum.GetValues<InventoryItemType>())
			{
				CallDeferred(MethodName.UpdateInventory, (int)itemType, 100000);
			}
		}

		CallDeferred(MethodName.UpdateInventory, (int)InventoryItemType.CannonBall, 10);
		CallDeferred(MethodName.UpdateInventory, (int)InventoryItemType.Coin, Configuration.StartingCoin);
	}

	private void RandomSpawn(int startXRange, int startYRange)
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		GlobalPosition = new Vector3(
			rng.Randf() * startYRange - startYRange / 2,
			GlobalPosition.Y,
			rng.Randf() * startXRange - startXRange / 2
		);
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
		if (_inventory.GetItemCount(InventoryItemType.CannonBall) <= 0)
		{
			GD.PrintErr($"{Name} tried to fire cannons but has no cannonballs!");
			_firedTimerCountdown = 0.1f;
			return;
		}

		UpdateInventory(InventoryItemType.CannonBall, -1);

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
			float targetSpeed = forwardInput * Stats.GetStat(PlayerStat.ShipMaxSpeed);
			_currentSpeed = Mathf.MoveToward(_currentSpeed, targetSpeed, Stats.GetStat(PlayerStat.ShipAcceleration) * (float)delta);
		}
		else
		{
			// Decelerate when no input (ship drifts to a stop)
			_currentSpeed = Mathf.MoveToward(_currentSpeed, 0.0f, Stats.GetStat(PlayerStat.ShipDeceleration) * (float)delta);
		}

		// Allow turning with responsive controls (no speed requirement)
		if (turnInput != 0.0f)
		{
			// Rotate the ship - simple and responsive
			RotateY(-turnInput * Stats.GetStat(PlayerStat.ShipTurnSpeed) * (float)delta);
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

	public void PurchaseComponent(Component Component)
	{
		if (!MakePurchase(Component.cost))
			return;

		OwnedComponents.Add(new OwnedComponent
		{
			Component = Component,
			isEquipped = false
		});

		UpdatePlayerStats();
	}

	public void ToggleComponentEquipped(Component Component)
	{
		for (int i = 0; i < OwnedComponents.Count; i++)
		{
			if (OwnedComponents[i].Component == Component)
			{
				OwnedComponents[i].isEquipped = !OwnedComponents[i].isEquipped;
				GD.Print($"{Name} toggled component {Component.name} to {(OwnedComponents[i].isEquipped ? "equipped" : "unequipped")}");
				UpdatePlayerStats();
				break;
			}
		}
	}

	public void UpdatePlayerStats()
	{
		Stats.ResetStats();

		foreach (var ownedComponent in OwnedComponents)
		{
			if (ownedComponent.isEquipped)
			{
				foreach (var statChange in ownedComponent.Component.statChanges)
				{
					Stats.ApplyStatChange(statChange);
				}
			}
		}
	}

	public bool CanMakePurchase(Dictionary<InventoryItemType, int> cost)
	{
		foreach (var item in cost)
		{
			if (_inventory.GetItemCount(item.Key) < item.Value)
			{
				return false;
			}
		}
		return true;
	}

	public bool MakePurchase(Dictionary<InventoryItemType, int> cost)
	{
		if (!CanMakePurchase(cost))
		{
			return false;
		}

		foreach (var item in cost)
		{
			UpdateInventory(item.Key, -item.Value);
		}
		return true;
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

		if (_inventory.GetItemCount(InventoryItemType.Coin) + price < 0)
		{
			GD.PrintErr($"Cannot afford transaction. Current Gold: {_inventory.GetItemCount(InventoryItemType.Coin)}, Price: {price}");
			return false;
		}

		_inventory.AddItem(item, amount);
		if (price != 0)
		{
			_inventory.AddItem(InventoryItemType.Coin, price);
			EmitSignal(SignalName.InventoryChanged, (int)InventoryItemType.Coin, _inventory.GetItemCount(InventoryItemType.Coin));
		}

		EmitSignal(SignalName.InventoryChanged, (int)item, _inventory.GetItemCount(item));
		return true;
	}

	public int GetInventoryCount(InventoryItemType item)
	{
		return _inventory.GetItemCount(item);
	}

	public Dictionary<InventoryItemType, int> GetInventory()
	{
		return _inventory.GetAll();
	}
}
