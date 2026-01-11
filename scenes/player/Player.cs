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

  // Ship recoil/rocking effect when firing
  private float _recoilRoll = 0.0f;
  private const float RecoilRollAmount = 0.40f; // Radians, tweak for more/less rocking
  private const float RecoilDecaySpeed = 2.5f; // How quickly the rocking fades (higher = faster)

  public bool isLimitedByCapacity = true;

  public readonly PlayerStats Stats = new();
  public System.Collections.Generic.List<OwnedComponent> OwnedComponents = [];

  public int FallAcceleration { get; set; } = 75;
  public int Health { get; set; } = 80;
  public int MaxHealth => (int)Stats.GetStat(PlayerStat.ShipHullStrength);

  [Export] public string Nickname { get; set; }

  [Export] public Node3D CannonPivotLeft;
  [Export] public Node3D CannonPivotRight;
  [Export] public MultiplayerSpawner ProjectileSpawner;
  [Export] public MultiplayerSpawner DeadPlayerSpawner;
  [Export] public Timer AutoHealTimer;

  // Water Physics Properties
  [ExportGroup("Water Physics")]
  [Export] public FastNoiseLite WaterNoiseResource { get; set; }
  [Export] public MeshInstance3D WaterPlane { get; set; }
  [Export] public float WaveHeight { get; set; } = 3.0f;
  [Export] public float WaveSpeed { get; set; } = 0.05f;
  [Export] public float WaterNoiseScale { get; set; } = 0.002f;
  [Export] public float ShipLength { get; set; } = 10.0f;
  [Export] public float VerticalOffset { get; set; } = -0.2f;
  [Export] public float WaterSmoothSpeed { get; set; } = 8.0f;

  private float _currentSpeed = 0.0f;
  private Vector3 _targetVelocity = Vector3.Zero;
  private readonly Inventory _inventory = new();
  private int _fireCoolDownInSeconds = 2;
  private double _firedTimerCountdown = 0;

  // Ship banking/tilt when turning
  private float _currentTurnInput = 0.0f;

  public override void _Ready()
  {
    // Only enable the camera for the player we control
    var camera = GetNodeOrNull<Camera3D>("CameraPivot/Camera3D");
    if (camera != null)
    {
      camera.Current = IsMultiplayerAuthority();
      GD.Print($"{Name}: Camera enabled = {camera.Current}");
    }

    if (IsMultiplayerAuthority())
    {
      var identity = GetNode<Identity>("/root/Identity");
      Nickname = identity.PlayerName;
      GD.Print($"{Name} is local player with nickname {Nickname}");
    }

    // Try to find water plane if not set
    if (WaterPlane == null)
    {
      WaterPlane = GetNodeOrNull<MeshInstance3D>("/root/Play/Ground/WaterPlane");
      if (WaterPlane != null)
      {
        GD.Print($"{Name}: Found WaterPlane at absolute path");
      }
      else
      {
        GD.PrintErr($"{Name}: Could not find WaterPlane!");
      }
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

    // Set up auto-heal timer
    if (AutoHealTimer != null && IsMultiplayerAuthority())
    {
      AutoHealTimer.WaitTime = 10.0; // Heal every 10 seconds
      AutoHealTimer.Timeout += OnAutoHealTimeout;
      AutoHealTimer.Start();
    }
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

      // Check for left cannon fire (Q key)
      if (Input.IsActionPressed("fire_left") && _firedTimerCountdown <= 0)
      {
        FireCannons(true); // true = fire from left side
      }
      // Check for right cannon fire (E key)
      else if (Input.IsActionPressed("fire_right") && _firedTimerCountdown <= 0)
      {
        FireCannons(false); // false = fire from right side
      }
    }
  }

  /// <summary>
  /// Fires a cannonball from either the left or right side of the ship.
  /// </summary>
  /// <param name="fromLeftSide">True to fire from the left cannon, false to fire from the right cannon</param>
  private void FireCannons(bool fromLeftSide)
  {
    // Check if player has cannonballs in inventory
    if (_inventory.GetItemCount(InventoryItemType.CannonBall) <= 0)
    {
      GD.PrintErr($"{Name} tried to fire cannons but has no cannonballs!");
      _firedTimerCountdown = 0.1f;
      return;
    }

    // Use one cannonball from inventory
    UpdateInventory(InventoryItemType.CannonBall, -1);

    // Start the cooldown timer
    _firedTimerCountdown = _fireCoolDownInSeconds;

    // Select which cannon pivot to use based on the parameter
    Node3D cannonPivot = fromLeftSide ? CannonPivotLeft : CannonPivotRight;

    // Determine firing direction: left cannon fires left, right cannon fires right (perpendicular to ship)
    Vector3 fireDirection = fromLeftSide ? -GlobalTransform.Basis.X : GlobalTransform.Basis.X;

    // Create spawn data for the cannonball
    var spawnData = new Godot.Collections.Dictionary
    {
      ["position"] = cannonPivot.GlobalPosition,  // Spawn at the selected cannon position
      ["direction"] = fireDirection,  // Fire perpendicular to the ship
      ["speed"] = _currentSpeed,  // Include the ship's current speed
      ["playerName"] = Name  // Track which player fired the cannonball
    };

    // Request the server to spawn the cannonball (works in multiplayer)
    Rpc(MethodName.RequestFireCannons, spawnData);

    // Emit signal so UI can react (e.g., show firing animation or sound)
    EmitSignal(SignalName.CannonFired);

    // Add rocking effect: left cannon rocks left, right cannon rocks right
    _recoilRoll += fromLeftSide ? -RecoilRollAmount : RecoilRollAmount;
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

    // Store current turn input for banking effect (used in ApplyWaterPhysics)
    _currentTurnInput = turnInput;

    // Move the ship in the direction it's facing
    // The pivot's forward direction is -Z in local space
    Vector3 forwardDirection = -GlobalTransform.Basis.Z;

    // Ground velocity - ship only moves forward/backward in facing direction
    _targetVelocity.X = forwardDirection.X * _currentSpeed;
    _targetVelocity.Z = forwardDirection.Z * _currentSpeed;

    // Vertical velocity - controlled by water physics, not gravity
    // Water physics sets the Y position directly, so we zero out Y velocity
    _targetVelocity.Y = 0;

    // Moving the character (horizontal movement only)
    Velocity = _targetVelocity;
    MoveAndSlide();

    // Apply water physics AFTER MoveAndSlide (bobbing and pitch)
    // This ensures our Y position and rotation don't get overwritten
    ApplyWaterPhysics(delta);
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
    RpcId(1, MethodName.ServerDeath, new Dictionary
    {
      ["peerId"] = Multiplayer.GetUniqueId(),
      ["nickname"] = Nickname,
      ["playerName"] = Name,
      ["position"] = GlobalPosition,
      ["items"] = _inventory.GetAll()
    });

    CallDeferred(MethodName.EmitSignal, SignalName.Death, Name);
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  public void ServerDeath(Variant spawnData)
  {
    if (!Multiplayer.IsServer()) return;
    DeadPlayerSpawner.Spawn(spawnData);
  }

  private void OnAutoHealTimeout()
  {
    if (!IsMultiplayerAuthority()) return;

    int healAmount = (int)Stats.GetStat(PlayerStat.HealthRegen);

    if (healAmount > 0 && Health < MaxHealth)
    {
      Health = Mathf.Min(Health + healAmount, MaxHealth);
      EmitSignal(SignalName.HealthUpdate, Health);
    }
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

  public int GetTotalEquippedComponents()
  {
    int count = 0;
    for (int i = 0; i < OwnedComponents.Count; i++)
    {
      if (OwnedComponents[i].isEquipped)
      {
        count++;
      }
    }
    return count;
  }

  public bool EquipComponent(Component Component)
  {
    if (GetTotalEquippedComponents() >= (int)Stats.GetStat(PlayerStat.ComponentCapacity))
      return false;

    for (int i = 0; i < OwnedComponents.Count; i++)
    {
      if (OwnedComponents[i].Component == Component && !OwnedComponents[i].isEquipped)
      {
        OwnedComponents[i].isEquipped = true;
        GD.Print($"{Name} equipped component {Component.name}");
        UpdatePlayerStats();
        return true;
      }
    }

    return false;
  }

  public bool UnEquipComponent(Component Component)
  {
    for (int i = 0; i < OwnedComponents.Count; i++)
    {
      if (OwnedComponents[i].Component == Component && OwnedComponents[i].isEquipped)
      {
        OwnedComponents[i].isEquipped = false;
        GD.Print($"{Name} unequipped component {Component.name}");
        UpdatePlayerStats();
        return true;
      }
    }

    return false;
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

  public bool CollectResource(InventoryItemType item, int amount)
  {
    float total = (float)amount;

    switch (item)
    {
      case InventoryItemType.Fish:
        total = amount * Stats.GetStat(PlayerStat.CollectionFish);
        break;
      case InventoryItemType.Wood:
        total = amount * Stats.GetStat(PlayerStat.CollectionWood);
        break;
      case InventoryItemType.Iron:
        total = amount * Stats.GetStat(PlayerStat.CollectionIron);
        break;
      default:
        break;
    }
    return UpdateInventory(item, Mathf.RoundToInt(total));
  }

  public bool BulkCollect(Dictionary<InventoryItemType, int> items)
  {
    foreach (var item in items)
    {
      if (!UpdateInventory(item.Key, item.Value))
      {
        return false;
      }
    }
    return true;
  }

  public bool UpdateInventory(InventoryItemType item, int amount)
  {
    return UpdateInventory(item, amount, 0);
  }

  public bool UpdateInventory(InventoryItemType item, int amount, int price = 0)
  {
    // Not a bug, we want to allow the user to exceed capacity if they do something in bulk
    if (amount > 0 && _inventory.GetTotalItemCount([InventoryItemType.Coin]) >= Stats.GetStat(PlayerStat.ShipCapacity))
    {
      GD.PrintErr($"{Name} cannot collect {amount} {item} - inventory full");
      return false;
    }

    if (_inventory.GetItemCount(InventoryItemType.Coin) + price < 0)
    {
      GD.PrintErr($"Cannot afford transaction. Current Gold: {_inventory.GetItemCount(InventoryItemType.Coin)}, Price: {price}");
      return false;
    }

    _inventory.UpdateItem(item, amount);
    if (price != 0)
    {
      _inventory.UpdateItem(InventoryItemType.Coin, price);
      EmitSignal(SignalName.InventoryChanged, (int)InventoryItemType.Coin, _inventory.GetItemCount(InventoryItemType.Coin));
    }

    EmitSignal(SignalName.InventoryChanged, (int)item, _inventory.GetItemCount(item));

    isLimitedByCapacity = _inventory.GetTotalItemCount([InventoryItemType.Coin]) > Stats.GetStat(PlayerStat.ShipCapacity);
    GD.Print($"{Name} updated inventory: {item} by {amount} (price: {price})");
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

  /// <summary>
  /// Applies water physics to make the boat bob and tilt with waves.
  /// This method samples the water height at the bow and stern of the ship,
  /// then adjusts vertical position and pitch rotation accordingly.
  /// </summary>
  private void ApplyWaterPhysics(float delta)
  {
    // Skip if water physics is not configured
    if (WaterNoiseResource == null || WaterPlane == null)
    {
      GD.PrintErr($"Water physics not configured! NoiseResource: {WaterNoiseResource != null}, WaterPlane: {WaterPlane != null}");
      return;
    }

    // Get current game time in seconds
    float time = Time.GetTicksMsec() / 1000.0f;
    Vector3 pos = GlobalPosition;

    // Get the ship's forward direction (Z axis in local space)
    Vector3 forwardDir = Transform.Basis.Z;

    // Calculate sampling points at bow (front) and stern (back)
    Vector3 bowPos = pos + forwardDir * (ShipLength / 2.0f);
    Vector3 sternPos = pos - forwardDir * (ShipLength / 2.0f);

    // Sample water heights at bow and stern
    float heightBow = GetWaterHeight(bowPos, time);
    float heightStern = GetWaterHeight(sternPos, time);
    float heightAvg = (heightBow + heightStern) / 2.0f;

    // Update vertical position to sit on water surface
    float planeY = WaterPlane?.GlobalPosition.Y ?? 0.0f;
    float targetY = heightAvg + planeY + VerticalOffset - 0.5f;

    // Smoothly interpolate to target Y position
    Vector3 currentPos = GlobalPosition;
    currentPos.Y = Mathf.Lerp(currentPos.Y, targetY, delta * WaterSmoothSpeed);
    GlobalPosition = currentPos;

    // Update pitch rotation based on wave slope
    float heightDiff = heightBow - heightStern;
    float targetPitch = -Mathf.Atan2(heightDiff, ShipLength);

    // Calculate roll (bank) based on turning and add recoil rocking
    float maxRollAngle = Mathf.DegToRad(5.0f); // Maximum 5 degrees of roll
    float targetRoll = _currentTurnInput * maxRollAngle + _recoilRoll;

    // Smoothly interpolate rotation
    Vector3 rotation = Rotation;
    rotation.X = Mathf.LerpAngle(rotation.X, targetPitch, delta * WaterSmoothSpeed);
    rotation.Z = Mathf.LerpAngle(rotation.Z, targetRoll, delta * WaterSmoothSpeed * 0.3f); // Slow, subtle banking
    Rotation = rotation;

    // Decay the recoil rocking over time so the ship returns to normal
    _recoilRoll = Mathf.Lerp(_recoilRoll, 0.0f, delta * RecoilDecaySpeed);
  }

  /// <summary>
  /// Calculates water height at a specific world position.
  /// This matches the shader's vertex displacement calculation.
  /// </summary>
  private float GetWaterHeight(Vector3 worldPos, float time)
  {
    // Convert world position to UV coordinates matching the shader
    float sampleX = (worldPos.X * WaterNoiseScale) + (time * WaveSpeed);
    float sampleZ = (worldPos.Z * WaterNoiseScale);

    // Sample the noise (multiply by 100 for proper scale)
    float noiseVal = WaterNoiseResource.GetNoise2D(sampleX * 100.0f, sampleZ * 100.0f);

    // Normalize from [-1, 1] to [0, 1] and scale by wave height
    return ((noiseVal + 1.0f) / 2.0f) * WaveHeight;
  }
}
