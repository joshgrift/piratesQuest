namespace PiratesQuest;

using Godot;
using PiratesQuest.AI;
using PiratesQuest.Attributes;
using PiratesQuest.Data;
using System.Collections.Generic;
using GodotDictionary = Godot.Collections.Dictionary;
using LootDictionary = Godot.Collections.Dictionary<PiratesQuest.Data.InventoryItemType, int>;

public partial class AiShip : CharacterBody3D, IDamageable
{
  [Export] public string DisplayName { get; set; } = "Raider";
  [Export] public Node3D CannonPivotLeft;
  [Export] public Node3D CannonPivotRight;
  [Export] public MultiplayerSynchronizer Synchronizer;
  [Export] public RayCast3D ForwardRay;
  [Export] public RayCast3D ForwardLeftRay;
  [Export] public RayCast3D ForwardRightRay;
  [Export] public RayCast3D WideLeftRay;
  [Export] public RayCast3D WideRightRay;
  [Export] public Node3D VisualRoot { get; set; }
  [Export] public Node3D RaiderVisualRoot { get; set; }
  [Export] public Node3D TraderVisualRoot { get; set; }

  [ExportGroup("Water Physics")]
  [Export] public NodePath WaterPlanePath { get; set; } = new("/root/Play/WaterPlane");
  [Export] public float ShipLength { get; set; } = 10.0f;
  [Export] public float VisualBobStrength { get; set; } = 0.35f;
  [Export] public float WaterSmoothSpeed { get; set; } = 7.0f;
  [Export] public bool ShowWaterDebug { get; set; } = false;

  public MultiplayerSpawner ProjectileSpawner { get; set; }
  public MultiplayerSpawner DeadPlayerSpawner { get; set; }
  public AiShipManager Manager { get; set; }
  public int Health { get; set; } = 100;
  public int MaxHealth => Mathf.RoundToInt(_definition.MaxHealth);
  public string ArchetypeId => _definition.Id;
  public string AllyTypeId => _definition.AllyTypeId;
  public bool IsDebugEnabled => _debugEnabled;
  public bool IsNavigationDebugEnabled => _navigationDebugEnabled;

  private AiShipDefinition _definition = AiShipDefinition.FromId("raider");
  private IAiShipController _controller;
  private readonly AiShipMemory _memory = new();
  private readonly Dictionary<InventoryItemType, int> _cargoManifest = [];
  private readonly List<Port> _ports = [];

  private Vector3 _spawnPoint;
  private Vector3 _targetVelocity = Vector3.Zero;
  private Vector3 _lastPosition = Vector3.Zero;
  private float _currentSpeed = 0.0f;
  private float _lastSpeed = 0.0f;
  private float _accelerationPitch = 0.0f;
  private float _currentTurnInput = 0.0f;
  private float _recoilRoll = 0.0f;
  private float _fireCooldownRemaining = 0.0f;
  private float _stuckTimer = 0.0f;
  private float _frontBlockedTimer = 0.0f;
  private Vector3 _stallAreaAnchor = Vector3.Zero;
  private float _stallAreaTimer = 0.0f;
  private float _stallAreaClearTimer = 0.0f;
  private float _lifetimeSeconds = 0.0f;
  private float _escapeTimerRemaining = 0.0f;
  private float _escapeTurnDirection = 1.0f;
  private bool _isSinking = false;
  private bool _controllerRemoved = false;
  private bool _debugEnabled = false;
  private bool _navigationDebugEnabled = false;
  private string _debugState = string.Empty;
  private bool _debugHasTargetShip = false;
  private float _debugDistanceToTargetShip = 0.0f;
  private bool _debugFrontBlocked = false;
  private bool _debugLeftBlocked = false;
  private bool _debugRightBlocked = false;
  private FloatingBody3D _floatingBody;
  private MeshInstance3D _patrolPointDebugMarker;
  private Label3D _stateDebugLabel;
  private readonly List<RayDebugVisual> _rayDebugVisuals = new();

  private const float RecoilRollAmount = 0.32f;
  private const float RecoilDecaySpeed = 2.4f;
  private const float EscapeReverseDurationSeconds = 1.35f;
  private const float EscapeForwardDurationSeconds = 1.15f;
  private const float EscapeTriggerBlockedSeconds = 0.55f;
  private const float StuckAreaHalfExtent = 10.0f;
  private const float StuckAreaKillSeconds = 10.0f;
  private const float StuckAreaClearResetSeconds = 1.5f;
  private const float LifetimeRespawnSeconds = 1800.0f;
  private const string HunterPatrolPointMemoryKey = "hunter.patrol_point";
  private static readonly Color DebugRayBlockedColor = new(1.0f, 0.34f, 0.24f, 1.0f);
  private static readonly Color DebugRayClearColor = new(0.2f, 0.95f, 0.66f, 1.0f);
  private static readonly Vector3 DebugLabelOffset = new(0.0f, 7.0f, 0.0f);
  private static readonly Vector3 DebugMarkerHeightOffset = new(0.0f, 1.2f, 0.0f);

  private sealed class RayDebugVisual
  {
    public RayCast3D Ray { get; }
    public MeshInstance3D Mesh { get; }
    public StandardMaterial3D Material { get; }

    public RayDebugVisual(RayCast3D ray, MeshInstance3D mesh, StandardMaterial3D material)
    {
      Ray = ray;
      Mesh = mesh;
      Material = material;
    }
  }

  public override void _Ready()
  {
    AddToGroup("ai_ships");

    _lastPosition = GlobalPosition;

    _floatingBody = new FloatingBody3D(this);
    _controller ??= _definition.CreateController(Manager?.ControllerServices ?? AiShipControllerServices.Empty);
    RefreshDebugVisuals();
  }

  public override void _ExitTree()
  {
    NotifyControllerRemovedOnce("scene_exit");

    // The patrol marker is added to the active scene instead of under this ship,
    // so Godot will not clean it up automatically when the ship dies.
    // We explicitly remove AI debug visuals here so death, despawn, and fail-safe
    // cleanup all behave the same way.
    CleanupDebugVisuals();
  }

  /// <summary>
  /// Called by Play when the spawner instantiates this ship on each peer.
  /// </summary>
  public void ConfigureFromSpawnData(GodotDictionary data)
  {
    string definitionId = data.ContainsKey("definitionId")
      ? data["definitionId"].AsString()
      : "raider";

    Vector3 spawnPosition = data.ContainsKey("position") ? data["position"].AsVector3() : Vector3.Zero;
    Vector3 spawnRotation = data.ContainsKey("rotation") ? data["rotation"].AsVector3() : Vector3.Zero;

    _definition = AiShipDefinition.FromId(definitionId);
    _controller = _definition.CreateController(Manager?.ControllerServices ?? AiShipControllerServices.Empty);

    Name = data.ContainsKey("name") ? data["name"].AsString() : $"ai_ship_{GetInstanceId()}";
    DisplayName = data.ContainsKey("displayName") ? data["displayName"].AsString() : _definition.DisplayName;
    Position = spawnPosition;
    Rotation = spawnRotation;
    _debugEnabled = data.ContainsKey("debug") && data["debug"].AsBool();

    _spawnPoint = spawnPosition;
    _lastPosition = spawnPosition;
    _stallAreaAnchor = spawnPosition;
    _lifetimeSeconds = 0.0f;
    Health = MaxHealth;
    _isSinking = false;
    _controllerRemoved = false;

    _memory.Clear();
    _memory.Set("ship_id", Name);

    _cargoManifest.Clear();
    foreach (var entry in _definition.CargoManifest)
      _cargoManifest[entry.Key] = entry.Value;

    ApplyVisualType();
    RefreshDebugVisuals();
  }

  public override void _PhysicsProcess(double delta)
  {
    if (!IsMultiplayerAuthority() || _isSinking)
      return;

    if (_fireCooldownRemaining > 0.0f)
      _fireCooldownRemaining = Mathf.Max(0.0f, _fireCooldownRemaining - (float)delta);

    _lifetimeSeconds += (float)delta;
    if (_lifetimeSeconds >= LifetimeRespawnSeconds)
    {
      FailSafeRespawn("lifetime expired after 30 minutes");
      return;
    }

    Port nearestPort = FindNearestPort();
    Port[] availablePorts = GetPorts();
    AiShipContact[] nearbyShips = BuildNearbyShipContacts();

    float traveled = GlobalPosition.DistanceTo(_lastPosition);
    _lastPosition = GlobalPosition;

    bool isTryingToMove = _currentSpeed > 2.5f;
    if (isTryingToMove && traveled < 0.05f)
      _stuckTimer += (float)delta;
    else
      _stuckTimer = 0.0f;

    bool frontBlocked = ForwardRay?.IsColliding() ?? false;
    bool frontLeftBlocked = ForwardLeftRay?.IsColliding() ?? false;
    bool frontRightBlocked = ForwardRightRay?.IsColliding() ?? false;
    bool wideLeftBlocked = WideLeftRay?.IsColliding() ?? false;
    bool wideRightBlocked = WideRightRay?.IsColliding() ?? false;
    bool leftBlocked = frontLeftBlocked || wideLeftBlocked;
    bool rightBlocked = frontRightBlocked || wideRightBlocked;

    if (frontBlocked)
      _frontBlockedTimer += (float)delta;
    else
      _frontBlockedTimer = 0.0f;

    UpdateStallAreaTimer((float)delta, frontBlocked);
    UpdateEscapeState((float)delta, frontBlocked, leftBlocked, rightBlocked);

    _controller.SyncSceneMemory(
      _memory,
      isStuck: _stuckTimer >= 1.0f,
      isEscaping: _escapeTimerRemaining > 0.0f,
      isEscapeReversing: _escapeTimerRemaining > EscapeForwardDurationSeconds,
      escapeTurnDirection: _escapeTurnDirection
    );

    var context = new AiShipContext
    {
      ShipPosition = GlobalPosition,
      ShipBasis = GlobalTransform.Basis,
      CurrentSpeed = _currentSpeed,
      SpawnPoint = _spawnPoint,
      NearestPort = nearestPort,
      Ports = availablePorts,
      NearbyShips = nearbyShips,
      TerrainRays =
      [
        BuildTerrainRayReading(AiShipRayIds.Forward, ForwardRay),
        BuildTerrainRayReading(AiShipRayIds.ForwardLeft, ForwardLeftRay),
        BuildTerrainRayReading(AiShipRayIds.ForwardRight, ForwardRightRay),
        BuildTerrainRayReading(AiShipRayIds.WideLeft, WideLeftRay),
        BuildTerrainRayReading(AiShipRayIds.WideRight, WideRightRay),
      ]
    };

    AiShipContact debugTargetShip = context.FindNearestHostileShip();
    _debugHasTargetShip = debugTargetShip != null;
    _debugDistanceToTargetShip = debugTargetShip?.Distance ?? 0.0f;
    _debugFrontBlocked = AiNavigationHelpers.IsFrontBlocked(context);
    _debugLeftBlocked = AiNavigationHelpers.IsLeftBlocked(context);
    _debugRightBlocked = AiNavigationHelpers.IsRightBlocked(context);

    AiShipControlInput control = _controller.GetControl(context, _memory, delta);

    ApplyControl(control, (float)delta);
    UpdateDebugVisuals();
  }

  public void ForceRemoval(string reason)
  {
    if (_isSinking)
      return;

    _isSinking = true;
    NotifyControllerRemovedOnce(reason);
    QueueFree();
  }

  private void UpdateStallAreaTimer(float delta, bool frontBlocked)
  {
    bool isTroubled = frontBlocked || _stuckTimer > 0.0f || _escapeTimerRemaining > 0.0f;
    if (!isTroubled)
    {
      _stallAreaClearTimer += delta;
      if (_stallAreaClearTimer >= StuckAreaClearResetSeconds)
      {
        _stallAreaAnchor = GlobalPosition;
        _stallAreaTimer = 0.0f;
      }
      return;
    }

    _stallAreaClearTimer = 0.0f;

    if (_stallAreaTimer <= 0.0f)
      _stallAreaAnchor = GlobalPosition;

    bool stillInSameArea =
      Mathf.Abs(GlobalPosition.X - _stallAreaAnchor.X) <= StuckAreaHalfExtent &&
      Mathf.Abs(GlobalPosition.Z - _stallAreaAnchor.Z) <= StuckAreaHalfExtent;

    if (!stillInSameArea)
    {
      _stallAreaAnchor = GlobalPosition;
      _stallAreaTimer = 0.0f;
      return;
    }

    _stallAreaTimer += delta;
    if (_stallAreaTimer >= StuckAreaKillSeconds)
      FailSafeRespawn("stuck in the same 20x20 area for 10 seconds");
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  public void TakeDamage(int amount)
  {
    if (!IsMultiplayerAuthority() || _isSinking)
      return;

    Health -= amount;
    if (Health <= 0)
      Sink();
  }

  private void ApplyControl(AiShipControlInput control, float delta)
  {
    _debugState = control.DebugState ?? string.Empty;

    float throttle = Mathf.Clamp(control.Throttle, -1.0f, 1.0f);
    float turn = Mathf.Clamp(control.Turn, -1.0f, 1.0f);

    if (throttle != 0.0f)
    {
      float targetSpeed = throttle * _definition.MaxSpeed;
      _currentSpeed = Mathf.MoveToward(_currentSpeed, targetSpeed, _definition.Acceleration * delta);
    }
    else
    {
      _currentSpeed = Mathf.MoveToward(_currentSpeed, 0.0f, _definition.Deceleration * delta);
    }

    float speedDelta = _currentSpeed - _lastSpeed;
    _lastSpeed = _currentSpeed;

    _accelerationPitch = Mathf.Lerp(
      _accelerationPitch,
      Mathf.Clamp(speedDelta * 0.35f, Mathf.DegToRad(-7.0f), Mathf.DegToRad(7.0f)),
      delta * 9.0f
    );

    if (turn != 0.0f)
      RotateY(-turn * _definition.TurnSpeed * delta);

    _currentTurnInput = turn;

    Vector3 forwardDirection = -GlobalTransform.Basis.Z;
    _targetVelocity.X = forwardDirection.X * _currentSpeed;
    _targetVelocity.Z = forwardDirection.Z * _currentSpeed;
    _targetVelocity.Y = 0.0f;

    Velocity = _targetVelocity;
    MoveAndSlide();
    ApplyWaterPhysics(delta);

    if (control.FireLeft)
      TryFireCannons(true);
    if (control.FireRight)
      TryFireCannons(false);
  }

  private void TryFireCannons(bool fromLeftSide)
  {
    if (_fireCooldownRemaining > 0.0f || ProjectileSpawner == null)
      return;

    Node3D cannonPivot = fromLeftSide ? CannonPivotLeft : CannonPivotRight;
    if (cannonPivot == null)
      return;

    Vector3 fireDirection = fromLeftSide ? -GlobalTransform.Basis.X : GlobalTransform.Basis.X;

    var spawnData = new GodotDictionary
    {
      ["position"] = cannonPivot.GlobalPosition,
      ["direction"] = fireDirection,
      ["speed"] = _currentSpeed + _definition.ProjectileBonusSpeed,
      ["playerName"] = Name,
      ["damage"] = Mathf.RoundToInt(_definition.AttackDamage)
    };

    ProjectileSpawner.Spawn(spawnData);
    _fireCooldownRemaining = _definition.FireCooldownSeconds;
    _recoilRoll += fromLeftSide ? -RecoilRollAmount : RecoilRollAmount;
    Rpc(MethodName.PlayCannonSound);
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  private void PlayCannonSound()
  {
    if (Multiplayer.IsServer() && !IsMultiplayerAuthority())
      return;

    var tempAudio = new AudioStreamPlayer3D();
    tempAudio.Stream = GD.Load<AudioStream>("res://art/sounds/jcsounds/Misc Sfx/sfx_cannon_fire_01.wav");
    tempAudio.VolumeDb = 17.0f;
    tempAudio.MaxDistance = 100.0f;

    GetTree().Root.AddChild(tempAudio);
    tempAudio.GlobalPosition = GlobalPosition;
    tempAudio.Finished += () => tempAudio.QueueFree();
    tempAudio.Play();
  }

  private AiShipContact[] BuildNearbyShipContacts()
  {
    var contacts = new List<AiShipContact>();

    foreach (Node node in GetTree().GetNodesInGroup("players"))
    {
      if (node is not Player player)
        continue;
      if (player.State == PlayerState.Dead || player.IsInPort)
        continue;

      float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
      if (distance > AiShipWorldSettings.ShipDiscoveryRange)
        continue;

      contacts.Add(new AiShipContact
      {
        IsPlayer = true,
        IsAllied = false,
        IsThreat = distance <= _definition.ShipAvoidanceRange,
        Distance = distance,
        Position = player.GlobalPosition,
      });
    }

    foreach (Node node in GetTree().GetNodesInGroup("ai_ships"))
    {
      if (node == this)
        continue;
      if (node is not AiShip aiShip)
        continue;
      if (aiShip._isSinking)
        continue;

      float distance = GlobalPosition.DistanceTo(aiShip.GlobalPosition);
      if (distance > AiShipWorldSettings.ShipDiscoveryRange)
        continue;

      bool isAllied = aiShip.AllyTypeId == AllyTypeId;
      contacts.Add(new AiShipContact
      {
        IsPlayer = false,
        IsAllied = isAllied,
        IsThreat = !isAllied && distance <= _definition.ShipAvoidanceRange,
        Distance = distance,
        Position = aiShip.GlobalPosition,
      });
    }

    return contacts.ToArray();
  }

  private static AiShipTerrainRay BuildTerrainRayReading(string id, RayCast3D ray)
  {
    float maxDistance = ray?.TargetPosition.Length() ?? float.MaxValue;
    bool isBlocked = ray?.IsColliding() ?? false;
    float distance = maxDistance;

    if (isBlocked)
      distance = ray.GlobalPosition.DistanceTo(ray.GetCollisionPoint());

    return new AiShipTerrainRay
    {
      Id = id,
      IsBlocked = isBlocked,
      Distance = distance,
      MaxDistance = maxDistance,
    };
  }

  private Port FindNearestPort()
  {
    Port best = null;
    float bestDistance = float.MaxValue;

    foreach (Node node in GetTree().GetNodesInGroup("ports"))
    {
      if (node is not Port port)
        continue;

      float distance = GlobalPosition.DistanceTo(port.GlobalPosition);
      if (distance < bestDistance)
      {
        best = port;
        bestDistance = distance;
      }
    }

    return best;
  }

  private Port[] GetPorts()
  {
    _ports.Clear();

    foreach (Node node in GetTree().GetNodesInGroup("ports"))
    {
      if (node is Port port)
        _ports.Add(port);
    }

    return _ports.ToArray();
  }

  private void UpdateEscapeState(
    float delta,
    bool frontBlocked,
    bool leftBlocked,
    bool rightBlocked)
  {
    if (_escapeTimerRemaining > 0.0f)
    {
      _escapeTimerRemaining = Mathf.Max(0.0f, _escapeTimerRemaining - delta);
      return;
    }

    bool frontBlockedLongEnough = frontBlocked && _frontBlockedTimer >= EscapeTriggerBlockedSeconds;
    bool stuck = _stuckTimer >= 1.0f;
    if (!frontBlockedLongEnough && !stuck)
      return;

    _escapeTurnDirection = ChooseEscapeTurnDirection(leftBlocked, rightBlocked);
    _escapeTimerRemaining = EscapeReverseDurationSeconds + EscapeForwardDurationSeconds;
  }

  private void RefreshDebugVisuals()
  {
    if (!IsInsideTree())
      return;

    bool isServer = Multiplayer.IsServer();
    bool shouldShowStateDebug = _debugEnabled && isServer;
    bool shouldShowNavigationDebug = _navigationDebugEnabled && isServer;

    if (!shouldShowStateDebug)
    {
      CleanupStateDebugVisuals();
    }
    else if (_stateDebugLabel == null)
    {
      _stateDebugLabel = AiDebugVisuals.CreateStateLabel("AiStateDebugLabel", DebugLabelOffset);
      AddChild(_stateDebugLabel);
    }

    if (!shouldShowNavigationDebug)
    {
      CleanupNavigationDebugVisuals();
    }
    else
    {
      EnsureNavigationDebugVisuals();
    }
  }

  private void CleanupDebugVisuals()
  {
    CleanupStateDebugVisuals();
    CleanupNavigationDebugVisuals();
  }

  private void CleanupStateDebugVisuals()
  {
    if (IsInstanceValid(_stateDebugLabel))
      _stateDebugLabel.QueueFree();
    _stateDebugLabel = null;
  }

  private void CleanupNavigationDebugVisuals()
  {
    if (IsInstanceValid(_patrolPointDebugMarker))
      _patrolPointDebugMarker.QueueFree();
    _patrolPointDebugMarker = null;

    foreach (RayDebugVisual rayDebug in _rayDebugVisuals)
    {
      if (IsInstanceValid(rayDebug.Mesh))
        rayDebug.Mesh.QueueFree();
    }
    _rayDebugVisuals.Clear();
  }

  private void EnsureNavigationDebugVisuals()
  {
    if (_patrolPointDebugMarker == null)
    {
      _patrolPointDebugMarker = AiDebugVisuals.CreatePointMarker(
        "PatrolPointDebugMarker",
        new Color(0.25f, 0.95f, 0.65f),
        radius: 0.8f
      );
      GetTree().CurrentScene?.AddChild(_patrolPointDebugMarker);
    }

    if (_rayDebugVisuals.Count == 0)
    {
      TryCreateRayDebugVisual("ForwardRayDebug", ForwardRay);
      TryCreateRayDebugVisual("ForwardLeftRayDebug", ForwardLeftRay);
      TryCreateRayDebugVisual("ForwardRightRayDebug", ForwardRightRay);
      TryCreateRayDebugVisual("WideLeftRayDebug", WideLeftRay);
      TryCreateRayDebugVisual("WideRightRayDebug", WideRightRay);
    }
  }

  private void TryCreateRayDebugVisual(string name, RayCast3D ray)
  {
    if (ray == null)
      return;

    var material = new StandardMaterial3D
    {
      AlbedoColor = DebugRayClearColor,
      ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
      Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
    };

    var rayMesh = new MeshInstance3D
    {
      Name = name,
      TopLevel = true,
      Mesh = new ImmediateMesh(),
      MaterialOverride = material,
      CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
    };
    GetTree().CurrentScene?.AddChild(rayMesh);

    _rayDebugVisuals.Add(new RayDebugVisual(ray, rayMesh, material));
  }

  private void UpdateDebugVisuals()
  {
    if (!Multiplayer.IsServer())
      return;

    if (!IsInsideTree())
      return;

    RefreshDebugVisuals();

    if (_debugEnabled && _stateDebugLabel != null)
      _stateDebugLabel.Text = BuildDebugText();

    if (_navigationDebugEnabled)
      UpdateNavigationDebugVisuals();
  }

  private void UpdateNavigationDebugVisuals()
  {
    if (_memory.TryGet<Vector3>(HunterPatrolPointMemoryKey, out Vector3 patrolPoint))
    {
      if (_patrolPointDebugMarker != null)
      {
        _patrolPointDebugMarker.Visible = true;
        _patrolPointDebugMarker.GlobalPosition = patrolPoint + DebugMarkerHeightOffset;
      }
    }
    else if (_patrolPointDebugMarker != null)
    {
      _patrolPointDebugMarker.Visible = false;
    }

    foreach (RayDebugVisual rayDebug in _rayDebugVisuals)
    {
      UpdateRayDebugVisual(rayDebug);
    }
  }

  private static void UpdateRayDebugVisual(RayDebugVisual rayDebug)
  {
    if (!IsInstanceValid(rayDebug.Ray) || !IsInstanceValid(rayDebug.Mesh))
      return;

    Vector3 start = rayDebug.Ray.GlobalPosition;
    bool blocked = rayDebug.Ray.IsColliding();
    Vector3 end = blocked
      ? rayDebug.Ray.GetCollisionPoint()
      : rayDebug.Ray.ToGlobal(rayDebug.Ray.TargetPosition);

    rayDebug.Material.AlbedoColor = blocked ? DebugRayBlockedColor : DebugRayClearColor;

    if (rayDebug.Mesh.Mesh is not ImmediateMesh lineMesh)
      return;

    lineMesh.ClearSurfaces();
    lineMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
    lineMesh.SurfaceAddVertex(start);
    lineMesh.SurfaceAddVertex(end);
    lineMesh.SurfaceEnd();
  }

  /// <summary>
  /// Server-side AI debug toggle used by the AI debug menu.
  /// </summary>
  public void SetDebugEnabled(bool enabled)
  {
    if (_debugEnabled == enabled)
      return;

    _debugEnabled = enabled;
    RefreshDebugVisuals();
    UpdateDebugVisuals();
  }

  /// <summary>
  /// Server-side navigation debug toggle for patrol-point and terrain-ray visuals.
  /// </summary>
  public void SetNavigationDebugEnabled(bool enabled)
  {
    if (_navigationDebugEnabled == enabled)
      return;

    _navigationDebugEnabled = enabled;
    RefreshDebugVisuals();
    UpdateDebugVisuals();
  }

  public string BuildDebugText()
  {
    string targetText = _debugHasTargetShip
      ? $"Ship {_debugDistanceToTargetShip:0.0}"
      : "None";
    string obstacleFlags = $"{(_debugFrontBlocked ? "F" : "-")}{(_debugLeftBlocked ? "L" : "-")}{(_debugRightBlocked ? "R" : "-")}";
    string escapeMode = _escapeTimerRemaining > 0.0f
      ? (_escapeTimerRemaining > EscapeForwardDurationSeconds ? "Reverse" : "Forward")
      : "None";

    return string.Join('\n', [
      DisplayName,
      $"State: {_debugState}",
      $"Target: {targetText}",
      $"Speed: {_currentSpeed:0.0}/{_definition.MaxSpeed:0.0}  Turn: {_currentTurnInput:0.00}",
      $"Blocked: {obstacleFlags}  Front: {_frontBlockedTimer:0.00}s",
      $"Stuck: {_stuckTimer:0.00}s  Escape: {escapeMode} {_escapeTimerRemaining:0.00}s",
      $"Trap: {_stallAreaTimer:0.00}s  Clear: {_stallAreaClearTimer:0.00}s",
      $"Trap Box: ({_stallAreaAnchor.X:0}, {_stallAreaAnchor.Z:0}) +/- {StuckAreaHalfExtent:0}",
      $"Life: {_lifetimeSeconds:0}s / {LifetimeRespawnSeconds:0}s",
      $"Fire Cd: {_fireCooldownRemaining:0.00}s"
    ]);
  }

  private void FailSafeRespawn(string reason)
  {
    if (_isSinking)
      return;

    _isSinking = true;
    GD.Print($"{Name}: {reason}, forcing respawn");
    NotifyControllerRemovedOnce("forced_respawn");

    Manager?.RequestImmediateRefill();

    QueueFree();
  }

  private float ChooseEscapeTurnDirection(bool leftBlocked, bool rightBlocked)
  {
    if (leftBlocked && !rightBlocked)
      return 1.0f;

    if (rightBlocked && !leftBlocked)
      return -1.0f;

    // If both sides look similar, don't always prefer the same side. Flipping
    // here gives the next recovery attempt a different angle of attack.
    _escapeTurnDirection *= -1.0f;
    return _escapeTurnDirection;
  }

  private void Sink()
  {
    if (_isSinking)
      return;

    _isSinking = true;
    NotifyControllerRemovedOnce("ship_sunk");

    if (DeadPlayerSpawner != null && _cargoManifest.Count > 0)
    {
      var itemDict = new LootDictionary();
      foreach (var entry in _cargoManifest)
        itemDict[entry.Key] = entry.Value;

      var spawnData = new GodotDictionary
      {
        ["position"] = GlobalPosition,
        ["items"] = itemDict,
        ["nickname"] = DisplayName,
        ["playerName"] = Name,
      };

      DeadPlayerSpawner.Spawn(spawnData);
    }

    Manager?.RequestImmediateRefill();
    QueueFree();
  }

  private void NotifyControllerRemovedOnce(string reason)
  {
    if (_controllerRemoved)
      return;

    _controllerRemoved = true;
    _controller?.OnRemoved(_memory, reason);
  }

  private float CalculateRayDistanceFraction(RayCast3D ray)
  {
    if (ray == null)
      return 1.0f;

    float maxDistance = ray.TargetPosition.Length();
    if (maxDistance <= 0.001f)
      return 1.0f;

    if (!ray.IsColliding())
      return 1.0f;

    float hitDistance = ray.GlobalPosition.DistanceTo(ray.GetCollisionPoint());
    return Mathf.Clamp(hitDistance / maxDistance, 0.0f, 1.0f);
  }

  public void OnDeath()
  {
    Sink();
  }

  private void ApplyWaterPhysics(float delta)
  {
    if (!_floatingBody.Apply(
      VisualRoot,
      WaterPlanePath,
      ShipLength,
      VisualBobStrength,
      WaterSmoothSpeed,
      ShowWaterDebug,
      _accelerationPitch,
      _currentTurnInput * Mathf.DegToRad(4.0f) + _recoilRoll,
      delta
    ))
      return;

    _recoilRoll = Mathf.Lerp(_recoilRoll, 0.0f, delta * RecoilDecaySpeed);
  }

  private void ApplyVisualType()
  {
    if (RaiderVisualRoot != null)
      RaiderVisualRoot.Visible = _definition.VisualType == AiShipVisualType.RaiderBlack;

    if (TraderVisualRoot != null)
      TraderVisualRoot.Visible = _definition.VisualType == AiShipVisualType.PlayerWhite;
  }
}
