namespace PiratesQuest;

using PiratesQuest.Attributes;
using Godot;
using Godot.Collections;
using PiratesQuest.Data;
using System;
using System.Text;
using System.Text.Json;

public class OwnedComponent
{
  public Component Component;
  public bool isEquipped;
}

/// <summary>
/// Represents the current state of the player.
/// Used to disable certain actions like moving, shooting, and taking damage when dead.
/// </summary>
public enum PlayerState
{
  Alive,
  Dead
}

public partial class Player : CharacterBody3D, ICanCollect, IDamageable
{
  [Signal] public delegate void InventoryChangedEventHandler(InventoryItemType itemType, int newAmount, int change);
  [Signal] public delegate void CannonFiredEventHandler();
  [Signal] public delegate void CannonReadyToFireEventHandler();
  [Signal] public delegate void DeathEventHandler(string playerName);
  [Signal] public delegate void HealthUpdateEventHandler(int newHealth);

  // Ship recoil/rocking effect when firing
  private float _recoilRoll = 0.0f;
  private const float RecoilRollAmount = 0.40f; // Radians, tweak for more/less rocking
  private const float RecoilDecaySpeed = 2.5f; // How quickly the rocking fades (higher = faster)

  public bool isLimitedByCapacity = true;
  public string UserId { get; set; }
  public string LastSyncedStateJson { get; set; }

  public readonly PlayerStats Stats = new();
  public System.Collections.Generic.List<OwnedComponent> OwnedComponents = [];

  // ── Vault ────────────────────────────────────────────────────────
  // Each player can build one vault at a single port.
  // Null VaultPortName means no vault exists yet.

  public string VaultPortName { get; set; }
  public int VaultLevel { get; set; }
  public System.Collections.Generic.Dictionary<InventoryItemType, int> VaultItems { get; set; } = new();

  // Capacities per vault level (index 0 = unused, 1-5 = levels)
  public static readonly int[] VaultItemCapacity = [0, 500, 1000, 2000, 4000, 6000];
  public static readonly int[] VaultGoldCapacity = [0, 2000, 4000, 8000, 16000, 32000];
  public const int VaultMaxLevel = 5;

  // Build cost: what it takes to construct a level-1 vault
  public static readonly Dictionary<InventoryItemType, int> VaultBuildCost = new()
  {
    { InventoryItemType.Wood, 50 },
    { InventoryItemType.Iron, 25 },
    { InventoryItemType.Coin, 100 },
  };

  /// <summary>
  /// Returns the upgrade cost to go from the given level to level+1.
  /// Costs triple each tier: base * 3^(level-1).
  /// </summary>
  public static Dictionary<InventoryItemType, int> GetVaultUpgradeCost(int currentLevel)
  {
    int multiplier = (int)Math.Pow(3, currentLevel - 1);
    return new Dictionary<InventoryItemType, int>
    {
      { InventoryItemType.Wood, 100 * multiplier },
      { InventoryItemType.Iron, 50 * multiplier },
      { InventoryItemType.Coin, 200 * multiplier },
    };
  }

  // Current state of the player - controls whether they can move, shoot, or take damage
  [Export] public PlayerState State { get; private set; } = PlayerState.Alive;
  // True while this ship is inside a port docking area.
  // We use this to disable incoming damage in safe zones.
  [Export] public bool IsInPort { get; private set; } = false;

  // ICanCollect.CanCollect - player can only collect items when alive
  public bool CanCollect => State == PlayerState.Alive;

  public int FallAcceleration { get; set; } = 75;
  public int Health { get; set; } = 100;
  public int MaxHealth => (int)Stats.GetStat(PlayerStat.ShipHullStrength);

  [Export] public string Nickname { get; set; }

  [Export] public Node3D CannonPivotLeft;
  [Export] public Node3D CannonPivotRight;
  [Export] public MultiplayerSpawner ProjectileSpawner;
  [Export] public MultiplayerSpawner DeadPlayerSpawner;
  [Export] public Timer AutoHealTimer;

  [Export] private AudioStreamPlayer3D _cannonSoundPlayer;

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

  [Export]
  public int TrophyCount
  {
    get => _inventory.GetItemCount(InventoryItemType.Trophy);
    set => _inventory.SetItem(InventoryItemType.Trophy, value);
  }

  // Simple movement state
  private float _currentSpeed = 0.0f;       // Current forward/backward speed
  private float _lastSpeed = 0.0f;          // Speed in previous frame
  private float _accelerationPitch = 0.0f;  // Extra pitch from speeding up / slowing down

  private Vector3 _targetVelocity = Vector3.Zero;
  private readonly Inventory _inventory = new();
  private int _fireCoolDownInSeconds = 2;
  private double _firedTimerCountdown = 0;

  // Ship banking/tilt when turning
  private float _currentTurnInput = 0.0f;

  // Track whether we're currently playing the creaking sound
  // This prevents us from starting the sound over and over every frame
  private bool _isCreakingPlaying = false;

  public override void _Ready()
  {
    Health = MaxHealth;

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

      var jwt = Configuration.GetUserToken();
      if (!string.IsNullOrWhiteSpace(jwt))
        RpcId(1, MethodName.RegisterAuth, jwt);

      var syncTimer = new Timer { WaitTime = 5.0, Autostart = true };
      syncTimer.Timeout += OnSyncTimeout;
      AddChild(syncTimer);
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
    // Can't shoot while dead (waiting to respawn)
    if (State == PlayerState.Dead) return;

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
      ["position"] = cannonPivot.GlobalPosition,
      ["direction"] = fireDirection,
      ["speed"] = _currentSpeed + Stats.GetStat(PlayerStat.AttackRange),
      ["playerName"] = Name,
      ["damage"] = Stats.GetStat(PlayerStat.AttackDamage)
    };

    Rpc(MethodName.RequestFireCannons, spawnData);

    // Play cannon sound on ALL clients (not just locally)
    Rpc(MethodName.PlayCannonSound);

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

  /// <summary>
  /// Plays the cannon firing sound at this ship's position.
  /// Called via RPC so all clients hear it from the correct location.
  /// </summary>
  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  private void PlayCannonSound()
  {
    // Don't play audio on the server - only clients have audio output
    if (Multiplayer.IsServer() && !IsMultiplayerAuthority()) return;

    // Create a temporary 3D audio player at THIS ship's position.
    // We can't rely on _cannonSoundPlayer because [Export] fields may not
    // be set up correctly on non-local player instances.
    var tempAudio = new AudioStreamPlayer3D();
    tempAudio.Stream = GD.Load<AudioStream>("res://art/sounds/jcsounds/Misc Sfx/sfx_cannon_fire_01.wav");
    tempAudio.VolumeDb = 20.0f;
    tempAudio.MaxDistance = 100.0f;

    // Add to scene tree first (required for GlobalPosition)
    GetTree().Root.AddChild(tempAudio);
    tempAudio.GlobalPosition = GlobalPosition;

    // Play and auto-delete when finished
    tempAudio.Finished += () => tempAudio.QueueFree();
    tempAudio.Play();
  }

  private void UpdateMovement(float delta)
  {
    // Can't move while dead (waiting to respawn)
    if (State == PlayerState.Dead) return;

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

    // Play creaking sound when the player is actively sailing (W or turning)
    // This gives audio feedback that the ship is under strain
    UpdateCreakingSound(forwardInput != 0.0f || turnInput != 0.0f);

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

    // --- Simple acceleration-based pitch (nose up/down) --------------------
    // How much our speed changed this frame.
    float speedDelta = _currentSpeed - _lastSpeed;
    _lastSpeed = _currentSpeed;

    // Map change in speed to a small pitch offset in radians.
    // Empirically tuned for this ship:
    //   - slowing down (speedDelta < 0)  => lean FORWARD
    //   - speeding up  (speedDelta > 0)  => lean BACK
    float targetAccelPitch = Mathf.Clamp(
      speedDelta * 0.4f,
      Mathf.DegToRad(-8.0f),
      Mathf.DegToRad(8.0f)
    );

    // Smooth so it doesn't snap every frame.
    _accelerationPitch = Mathf.Lerp(
      _accelerationPitch,
      targetAccelPitch,
      delta * 10.0f
    );

    // --- Turning (left/right) ------------------------------------------------
    // By default, turning is based directly on the left/right input.
    float effectiveTurnInput = turnInput;

    // When backing up, players usually expect steering to flip:
    //   - Pressing "left" while REVERSING should turn the ship visually left,
    //     relative to the direction of movement (like a car in reverse).
    // IMPORTANT: we only want this when the ship is actually MOVING backwards,
    // not just when the player taps the "move_back" key to slow down.
    //
    // _currentSpeed is our scalar "forward speed" along the ship's facing:
    //   - _currentSpeed > 0  => moving forward
    //   - _currentSpeed < 0  => moving backward
    if (_currentSpeed < 0.0f)
    {
      // Flip steering only while truly reversing.
      effectiveTurnInput = -effectiveTurnInput;
    }

    if (effectiveTurnInput != 0.0f)
    {
      // Rotate the ship. Negative sign keeps "move_right" turning the ship
      // to the right when moving forward.
      RotateY(-effectiveTurnInput * Stats.GetStat(PlayerStat.ShipTurnSpeed) * (float)delta);
    }

    // Store current turn input (after inversion) for banking effect
    // used in ApplyWaterPhysics.
    _currentTurnInput = effectiveTurnInput;

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

    // Can't take damage while dead (waiting to respawn)
    if (State == PlayerState.Dead) return;
    // Ports are safe zones: no damage while docked.
    if (IsInPort) return;

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

  /// <summary>
  /// Called by Port when this player enters or exits the docking area.
  /// </summary>
  public void SetInPort(bool value)
  {
    IsInPort = value;
  }

  public void OnDeath()
  {
    GD.Print($"dead: {Name}");

    // Set state to Dead - this disables movement, shooting, and taking damage
    State = PlayerState.Dead;

    // Calculate what items to drop:
    // - ALL trophies are dropped
    // - HALF of other items are dropped (rounded down)
    // The player keeps the other half of items but loses ALL components
    var itemsToDrop = new Dictionary<InventoryItemType, int>();
    var currentInventory = _inventory.GetAll();

    foreach (var item in currentInventory)
    {
      if (item.Key == InventoryItemType.Trophy)
      {
        // Drop ALL trophies
        itemsToDrop[item.Key] = item.Value;
      }
      else
      {
        // Drop HALF of other items (rounded down)
        int halfAmount = item.Value / 2;
        if (halfAmount > 0)
        {
          itemsToDrop[item.Key] = halfAmount;
        }
      }
    }

    // Remove dropped items from inventory and notify the HUD
    // For trophies: remove all
    // For other items: remove half (what we dropped)
    foreach (var item in itemsToDrop)
    {
      _inventory.UpdateItem(item.Key, -item.Value);
      // Emit signal so HUD updates to show new amounts
      EmitSignal(SignalName.InventoryChanged, (int)item.Key, _inventory.GetItemCount(item.Key), -item.Value);
    }

    // Clear ALL components - player loses all upgrades on death
    OwnedComponents.Clear();
    UpdatePlayerStats(); // Reset stats since no components are equipped

    GD.Print($"{Name} dropped items on death: {itemsToDrop}");
    GD.Print($"{Name} lost all components");

    RpcId(1, MethodName.ServerDeath, new Dictionary
    {
      ["peerId"] = Multiplayer.GetUniqueId(),
      ["nickname"] = Nickname,
      ["playerName"] = Name,
      ["position"] = GlobalPosition,
      ["items"] = itemsToDrop  // Only drop half items + all trophies
    });

    CallDeferred(MethodName.EmitSignal, SignalName.Death, Name);
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  public void ServerDeath(Variant spawnData)
  {
    if (!Multiplayer.IsServer()) return;
    DeadPlayerSpawner.Spawn(spawnData);
  }

  /// <summary>
  /// Respawns the player after death.
  /// Resets health to max, moves to a random spawn location, and makes visible again.
  /// Note: Components are already cleared in OnDeath, and half items are kept.
  /// </summary>
  public void Respawn()
  {
    if (!IsMultiplayerAuthority()) return;

    // Top up cannonballs and coins to starting defaults if below
    // This ensures respawned players always have minimum resources to play
    int currentCannonballs = _inventory.GetItemCount(InventoryItemType.CannonBall);
    int currentCoins = _inventory.GetItemCount(InventoryItemType.Coin);

    const int defaultCannonballs = 10;
    int defaultCoins = Configuration.StartingCoin;

    if (currentCannonballs < defaultCannonballs)
    {
      int topUp = defaultCannonballs - currentCannonballs;
      UpdateInventory(InventoryItemType.CannonBall, topUp);
    }

    if (currentCoins < defaultCoins)
    {
      int topUp = defaultCoins - currentCoins;
      UpdateInventory(InventoryItemType.Coin, topUp);
    }

    // Reset health to maximum
    Health = MaxHealth;
    EmitSignal(SignalName.HealthUpdate, Health);

    // Move to a new random spawn position
    RandomSpawn(100, 100);

    // Delay setting state to Alive by a short time (0.2 seconds)
    // This gives physics time to process the position change
    // and prevents picking up our own dropped items at the death location
    GetTree().CreateTimer(1.0f).Timeout += () => State = PlayerState.Alive;

    GD.Print($"{Name} has respawned with {Health} health");
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

  /// <summary>
  /// Purchases a component and automatically equips it if there's room.
  /// This removes the extra step of manually equipping after purchase.
  /// </summary>
  /// <param name="Component">The component to purchase</param>
  public void PurchaseComponent(Component Component)
  {
    // MakePurchase checks if we can afford it and deducts the cost
    if (!MakePurchase(Component.cost))
      return;

    // Check if we have room to equip this component right away
    // ComponentCapacity is the max number of components we can have active
    bool canEquipNow = GetTotalEquippedComponents() < (int)Stats.GetStat(PlayerStat.ComponentCapacity);

    // Add the component to our inventory
    // If we have room, equip it immediately so the player gets the benefit right away
    OwnedComponents.Add(new OwnedComponent
    {
      Component = Component,
      isEquipped = canEquipNow  // Auto-equip if there's room
    });

    GD.Print($"{Name} purchased component {Component.name} (auto-equipped: {canEquipNow})");
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
      EmitSignal(SignalName.InventoryChanged, (int)InventoryItemType.Coin, _inventory.GetItemCount(InventoryItemType.Coin), price);
    }

    EmitSignal(SignalName.InventoryChanged, (int)item, _inventory.GetItemCount(item), amount);

    if (item == InventoryItemType.Coin && amount > 0)
    {
      var audioManager = GetNode<AudioManager>("/root/AudioManager");
      audioManager.PlaySound("res://art/sounds/jcsounds/Misc Sfx/sfx_coin_clink_01.wav", volumeDb: -5.0f); // Slightly quieter
    }

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

  // ── Vault Operations ──────────────────────────────────────────────

  /// <summary>
  /// Build a brand-new vault at the given port. Deducts resources.
  /// Returns false if the player already has a vault or can't afford it.
  /// </summary>
  public bool BuildVault(string portName)
  {
    if (VaultPortName != null)
    {
      GD.PrintErr($"{Name}: Already has a vault at {VaultPortName}");
      return false;
    }

    if (!MakePurchase(VaultBuildCost))
      return false;

    VaultPortName = portName;
    VaultLevel = 1;
    VaultItems = new System.Collections.Generic.Dictionary<InventoryItemType, int>();
    GD.Print($"{Name}: Built vault at {portName}");
    return true;
  }

  /// <summary>
  /// Upgrade the vault to the next level. Deducts resources.
  /// </summary>
  public bool UpgradeVault()
  {
    if (VaultPortName == null || VaultLevel >= VaultMaxLevel)
      return false;

    var cost = GetVaultUpgradeCost(VaultLevel);
    if (!MakePurchase(cost))
      return false;

    VaultLevel++;
    GD.Print($"{Name}: Upgraded vault to level {VaultLevel}");
    return true;
  }

  /// <summary>
  /// Move items from the player's inventory into the vault.
  /// Respects the vault's item and gold capacity limits.
  /// </summary>
  public bool VaultDeposit(InventoryItemType item, int amount)
  {
    if (VaultPortName == null || amount <= 0)
      return false;

    // Check the player actually has the items
    if (_inventory.GetItemCount(item) < amount)
      return false;

    int currentVaultTotal = GetVaultItemCount();
    int currentVaultGold = GetVaultAmount(InventoryItemType.Coin);

    if (item == InventoryItemType.Coin)
    {
      // Gold has its own capacity limit
      if (currentVaultGold + amount > VaultGoldCapacity[VaultLevel])
        return false;
    }
    else
    {
      // Non-gold items share the item capacity
      int nonGoldVaultTotal = currentVaultTotal - currentVaultGold;
      if (nonGoldVaultTotal + amount > VaultItemCapacity[VaultLevel])
        return false;
    }

    // Move the items
    _inventory.UpdateItem(item, -amount);
    VaultItems[item] = GetVaultAmount(item) + amount;

    EmitSignal(SignalName.InventoryChanged, (int)item, _inventory.GetItemCount(item), -amount);
    GD.Print($"{Name}: Deposited {amount}x {item} into vault");
    return true;
  }

  /// <summary>
  /// Move items from the vault back into the player's inventory.
  /// </summary>
  public bool VaultWithdraw(InventoryItemType item, int amount)
  {
    if (VaultPortName == null || amount <= 0)
      return false;

    int vaultAmount = GetVaultAmount(item);
    if (vaultAmount < amount)
      return false;

    VaultItems[item] = vaultAmount - amount;
    if (VaultItems[item] <= 0)
      VaultItems.Remove(item);

    _inventory.UpdateItem(item, amount);
    EmitSignal(SignalName.InventoryChanged, (int)item, _inventory.GetItemCount(item), amount);
    GD.Print($"{Name}: Withdrew {amount}x {item} from vault");
    return true;
  }

  private int GetVaultItemCount()
  {
    int total = 0;
    foreach (var kvp in VaultItems)
      total += kvp.Value;
    return total;
  }

  private int GetVaultAmount(InventoryItemType item)
  {
    return VaultItems.TryGetValue(item, out var v) ? v : 0;
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
  private async void RegisterAuth(string jwt)
  {
    if (!Multiplayer.IsServer()) return;

    UserId = DecodeUserIdFromJwt(jwt);
    if (string.IsNullOrEmpty(UserId))
    {
      GD.PrintErr($"{Name}: Failed to decode userId from JWT");
      return;
    }

    GD.Print($"{Name}: Authenticated as user {UserId}");

    var stateJson = await ServerAPI.LoadPlayerStateAsync(Configuration.ServerId, UserId);
    if (stateJson != null)
    {
      LastSyncedStateJson = stateJson;
      int peerId = GetMultiplayerAuthority();
      RpcId(peerId, MethodName.ReceiveState, stateJson);
      GD.Print($"{Name}: Sent saved state to client (peer {peerId})");
    }
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
  private void ReceiveState(string stateJson)
  {
    if (!IsMultiplayerAuthority()) return;

    var dto = JsonSerializer.Deserialize<PlayerStateDto>(stateJson,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (dto == null) return;

    ApplyState(dto);
    GD.Print($"{Name}: Applied saved state from server");
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
  private void SyncStateToServer(string stateJson)
  {
    if (!Multiplayer.IsServer()) return;
    LastSyncedStateJson = stateJson;
  }

  private void OnSyncTimeout()
  {
    if (!IsMultiplayerAuthority()) return;
    var dto = SerializeState();
    var json = JsonSerializer.Serialize(dto);
    RpcId(1, MethodName.SyncStateToServer, json);
  }

  public PlayerStateDto SerializeState()
  {
    var dto = new PlayerStateDto
    {
      Health = Health,
      Position = [GlobalPosition.X, GlobalPosition.Y, GlobalPosition.Z],
      IsDead = State == PlayerState.Dead
    };

    var inventory = _inventory.GetAll();
    foreach (var item in inventory)
      dto.Inventory[item.Key.ToString()] = item.Value;

    foreach (var oc in OwnedComponents)
    {
      dto.Components.Add(new OwnedComponentDto
      {
        Name = oc.Component.name,
        IsEquipped = oc.isEquipped
      });
    }

    // Persist vault if the player has one
    if (VaultPortName != null)
    {
      var vaultDto = new VaultDto
      {
        PortName = VaultPortName,
        Level = VaultLevel,
        Items = new System.Collections.Generic.Dictionary<string, int>()
      };
      foreach (var kvp in VaultItems)
        vaultDto.Items[kvp.Key.ToString()] = kvp.Value;
      dto.Vault = vaultDto;
    }

    return dto;
  }

  public void ApplyState(PlayerStateDto dto)
  {
    foreach (var kvp in dto.Inventory)
    {
      if (Enum.TryParse<InventoryItemType>(kvp.Key, out var itemType))
      {
        _inventory.SetItem(itemType, kvp.Value);
        EmitSignal(SignalName.InventoryChanged, (int)itemType, kvp.Value, 0);
      }
    }

    OwnedComponents.Clear();
    foreach (var comp in dto.Components)
    {
      var found = System.Array.Find(GameData.Components, c => c.name == comp.Name);
      if (found != null)
      {
        OwnedComponents.Add(new OwnedComponent
        {
          Component = found,
          isEquipped = comp.IsEquipped
        });
      }
      else
      {
        GD.PrintErr($"{Name}: Unknown component '{comp.Name}' in saved state, skipping");
      }
    }
    UpdatePlayerStats();

    // Restore vault state
    if (dto.Vault != null)
    {
      VaultPortName = dto.Vault.PortName;
      VaultLevel = dto.Vault.Level;
      VaultItems = new System.Collections.Generic.Dictionary<InventoryItemType, int>();
      foreach (var kvp in dto.Vault.Items)
      {
        if (Enum.TryParse<InventoryItemType>(kvp.Key, out var itemType))
          VaultItems[itemType] = kvp.Value;
      }
      GD.Print($"{Name}: Restored vault at {VaultPortName} (level {VaultLevel})");
    }

    if (dto.IsDead)
    {
      Health = MaxHealth;
      RandomSpawn(100, 100);
      State = PlayerState.Alive;
      GD.Print($"{Name}: Was dead on save, respawning fresh with saved inventory");
    }
    else
    {
      Health = dto.Health;
      EmitSignal(SignalName.HealthUpdate, Health);
      GlobalPosition = new Vector3(dto.Position[0], dto.Position[1], dto.Position[2]);
    }
  }

  private static string DecodeUserIdFromJwt(string jwt)
  {
    var parts = jwt.Split('.');
    if (parts.Length != 3) return null;

    var payload = parts[1].Replace('-', '+').Replace('_', '/');
    switch (payload.Length % 4)
    {
      case 2: payload += "=="; break;
      case 3: payload += "="; break;
    }

    try
    {
      var bytes = Convert.FromBase64String(payload);
      var json = Encoding.UTF8.GetString(bytes);
      using var doc = JsonDocument.Parse(json);

      // ClaimTypes.NameIdentifier serializes to this URI in JWTs
      const string nameIdClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
      if (doc.RootElement.TryGetProperty(nameIdClaim, out var nameId))
        return nameId.GetString();

      return null;
    }
    catch (Exception ex)
    {
      GD.PrintErr($"JWT decode error: {ex.Message}");
      return null;
    }
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

    // Add extra pitch from acceleration / braking so the ship leans
    // slightly forward/backward when its speed changes aggressively.
    float combinedTargetPitch = targetPitch + _accelerationPitch;

    // Calculate roll (bank) based on turning and add recoil rocking
    float maxRollAngle = Mathf.DegToRad(5.0f); // Maximum 5 degrees of roll
    float targetRoll = _currentTurnInput * maxRollAngle + _recoilRoll;

    // Smoothly interpolate rotation
    Vector3 rotation = Rotation;
    rotation.X = Mathf.LerpAngle(rotation.X, combinedTargetPitch, delta * WaterSmoothSpeed);
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

  /// <summary>
  /// Starts or stops the ship creaking sound based on whether we're moving.
  ///
  /// This uses the AudioManager singleton with a unique loop name that includes
  /// our player name - this way each player has their own creaking sound that
  /// doesn't interfere with others.
  /// </summary>
  /// <param name="isMoving">True if the player is pressing movement keys</param>
  private void UpdateCreakingSound(bool isMoving)
  {
    // Only the local player should control their own creaking sound
    if (!IsMultiplayerAuthority()) return;

    var audioManager = GetNodeOrNull<AudioManager>("/root/AudioManager");
    if (audioManager == null) return;

    // Use a unique name for this player's creaking loop
    // This ensures each player's creaking doesn't conflict with others
    string loopName = $"creaking_{Name}";

    if (isMoving && !_isCreakingPlaying)
    {
      // Start creaking - the ship strains when you sail!
      audioManager.PlayLoop(
        loopName,
        "res://art/sounds/jcsounds/Ambiences (Loops)/amb_deck_creaks.wav",
        -18.0f  // Quiet but audible
      );
      _isCreakingPlaying = true;
    }
    else if (!isMoving && _isCreakingPlaying)
    {
      // Stop creaking when we stop moving
      audioManager.StopLoop(loopName);
      _isCreakingPlaying = false;
    }
  }

  /// <summary>
  /// Clean up audio when the player is removed from the scene.
  /// This prevents orphaned audio loops if the player disconnects.
  /// </summary>
  public override void _ExitTree()
  {
    if (IsMultiplayerAuthority() && Multiplayer.MultiplayerPeer?.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
      OnSyncTimeout();

    if (_isCreakingPlaying)
    {
      var audioManager = GetNodeOrNull<AudioManager>("/root/AudioManager");
      if (audioManager != null)
      {
        audioManager.StopLoop($"creaking_{Name}");
      }
    }
  }
}
