namespace PiratesQuest;

using Godot;
using PiratesQuest.AI;
using PiratesQuest.AI.hunterDeterministic;
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

  private AiShipDefinition _definition = AiShipDefinition.FromId("raider");
  private IAiShipController _controller;
  private readonly RandomNumberGenerator _rng = new();
  private readonly Dictionary<InventoryItemType, int> _cargoManifest = [];
  private readonly List<Player> _nearbyPlayers = [];
  private readonly List<Port> _ports = [];

  private Vector3 _spawnPoint;
  private Vector3 _patrolCenter;
  private Vector3 _patrolPoint;
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
  private bool _debugEnabled = false;
  private string _debugState = string.Empty;
  private bool _debugHasTargetPlayer = false;
  private float _debugDistanceToGoal = 0.0f;
  private bool _debugFrontBlocked = false;
  private bool _debugLeftBlocked = false;
  private bool _debugRightBlocked = false;
  private FloatingBody3D _floatingBody;
  private MeshInstance3D _patrolDebugMarker;
  private Label3D _stateDebugLabel;

  private const float RecoilRollAmount = 0.32f;
  private const float RecoilDecaySpeed = 2.4f;
  private const float EscapeReverseDurationSeconds = 1.35f;
  private const float EscapeForwardDurationSeconds = 1.15f;
  private const float EscapeTriggerBlockedSeconds = 0.55f;
  private const float StuckAreaHalfExtent = 10.0f;
  private const float StuckAreaKillSeconds = 10.0f;
  private const float StuckAreaClearResetSeconds = 1.5f;
  private const float LifetimeRespawnSeconds = 1800.0f;
  private static readonly Vector3 DebugLabelOffset = new(0.0f, 7.0f, 0.0f);
  private static readonly Vector3 DebugMarkerHeightOffset = new(0.0f, 1.2f, 0.0f);

  public override void _Ready()
  {
    AddToGroup("ai_ships");

    _rng.Randomize();
    _lastPosition = GlobalPosition;

    _floatingBody = new FloatingBody3D(this);
    _controller = _definition.CreateController();
    RefreshDebugVisuals();
  }

  /// <summary>
  /// Called by Play when the spawner instantiates this ship on each peer.
  /// </summary>
  public void ConfigureFromSpawnData(GodotDictionary data)
  {
    string definitionId = data.ContainsKey("definitionId")
      ? data["definitionId"].AsString()
      : "raider";

    _definition = AiShipDefinition.FromId(definitionId);
    _controller = _definition.CreateController();

    Name = data.ContainsKey("name") ? data["name"].AsString() : $"ai_ship_{GetInstanceId()}";
    DisplayName = data.ContainsKey("displayName") ? data["displayName"].AsString() : _definition.DisplayName;
    GlobalPosition = data.ContainsKey("position") ? data["position"].AsVector3() : Vector3.Zero;
    Rotation = data.ContainsKey("rotation") ? data["rotation"].AsVector3() : Vector3.Zero;
    _debugEnabled = data.ContainsKey("debug") && data["debug"].AsBool();

    _spawnPoint = GlobalPosition;
    _stallAreaAnchor = GlobalPosition;
    _lifetimeSeconds = 0.0f;
    _patrolCenter = PickPatrolCenter();
    _patrolPoint = PickPatrolPointInRange();
    Health = MaxHealth;

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

    var targetPlayer = FindNearestTargetPlayer();
    Port nearestPort = FindNearestPort();
    Port[] availablePorts = GetPorts();
    var nearbyThreatShip = FindNearestThreatShip();
    Vector3 goalPosition = targetPlayer != null ? targetPlayer.GlobalPosition : GetPatrolPoint();

    if (targetPlayer == null && GlobalPosition.DistanceTo(_patrolPoint) < 14.0f)
      _patrolPoint = PickPatrolPointInRange();

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
    float leftObstacleStrength = (frontLeftBlocked ? 1.0f : 0.0f) + (wideLeftBlocked ? 0.75f : 0.0f);
    float rightObstacleStrength = (frontRightBlocked ? 1.0f : 0.0f) + (wideRightBlocked ? 0.75f : 0.0f);

    if (frontBlocked)
      _frontBlockedTimer += (float)delta;
    else
      _frontBlockedTimer = 0.0f;

    UpdateStallAreaTimer((float)delta, frontBlocked, targetPlayer != null);
    UpdateEscapeState((float)delta, frontBlocked, leftBlocked, rightBlocked, targetPlayer == null);

    Vector3 localGoal = ToLocal(goalPosition);
    var context = new AiShipContext
    {
      ShipPosition = GlobalPosition,
      ShipBasis = GlobalTransform.Basis,
      CurrentSpeed = _currentSpeed,
      GoalPosition = goalPosition,
      LocalGoalPosition = localGoal,
      HasTargetPlayer = targetPlayer != null,
      DistanceToGoal = GlobalPosition.DistanceTo(goalPosition),
      FireRange = _definition.FireRange,
      PreferredCombatRange = _definition.PreferredCombatRange,
      GoalArrivalDistance = _definition.GoalArrivalDistance,
      FrontBlocked = frontBlocked,
      LeftBlocked = leftBlocked,
      RightBlocked = rightBlocked,
      LeftObstacleStrength = leftObstacleStrength,
      RightObstacleStrength = rightObstacleStrength,
      IsStuck = _stuckTimer >= 1.0f,
      IsEscaping = _escapeTimerRemaining > 0.0f,
      IsEscapeReversing = _escapeTimerRemaining > EscapeForwardDurationSeconds,
      EscapeTurnDirection = _escapeTurnDirection,
      NearestPort = nearestPort,
      Ports = availablePorts,
      HasNearbyThreatShip = nearbyThreatShip != null,
      NearbyThreatShipPosition = nearbyThreatShip?.GlobalPosition ?? Vector3.Zero,
      LocalNearbyThreatShipPosition = nearbyThreatShip != null ? ToLocal(nearbyThreatShip.GlobalPosition) : Vector3.Zero,
      DistanceToNearbyThreatShip = nearbyThreatShip != null ? GlobalPosition.DistanceTo(nearbyThreatShip.GlobalPosition) : float.MaxValue,
    };

    _debugHasTargetPlayer = context.HasTargetPlayer;
    _debugDistanceToGoal = context.DistanceToGoal;
    _debugFrontBlocked = context.FrontBlocked;
    _debugLeftBlocked = context.LeftBlocked;
    _debugRightBlocked = context.RightBlocked;

    ApplyControl(_controller.GetControl(context, delta), (float)delta);
    UpdateDebugVisuals();
  }

  private void UpdateStallAreaTimer(float delta, bool frontBlocked, bool hasTargetPlayer)
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

  private Player FindNearestTargetPlayer()
  {
    _nearbyPlayers.Clear();

    foreach (Node node in GetTree().GetNodesInGroup("players"))
    {
      if (node is not Player player)
        continue;
      if (player.State == PlayerState.Dead)
        continue;
      if (player.IsInPort)
        continue;

      float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
      if (distance <= _definition.DetectionRange)
        _nearbyPlayers.Add(player);
    }

    Player best = null;
    float bestDistance = float.MaxValue;

    foreach (Player player in _nearbyPlayers)
    {
      float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
      if (distance < bestDistance)
      {
        best = player;
        bestDistance = distance;
      }
    }

    return best;
  }

  private Node3D FindNearestThreatShip()
  {
    Node3D best = null;
    float bestDistance = float.MaxValue;

    foreach (Node node in GetTree().GetNodesInGroup("players"))
    {
      if (node is not Player player)
        continue;
      if (player.State == PlayerState.Dead || player.IsInPort)
        continue;

      float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
      if (distance > _definition.ShipAvoidanceRange || distance >= bestDistance)
        continue;

      best = player;
      bestDistance = distance;
    }

    foreach (Node node in GetTree().GetNodesInGroup("ai_ships"))
    {
      if (node == this)
        continue;
      if (node is not AiShip aiShip)
        continue;
      if (aiShip.AllyTypeId == AllyTypeId)
        continue;

      float distance = GlobalPosition.DistanceTo(aiShip.GlobalPosition);
      if (distance > _definition.ShipAvoidanceRange || distance >= bestDistance)
        continue;

      best = aiShip;
      bestDistance = distance;
    }

    return best;
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

  private Vector3 GetPatrolPoint()
  {
    return _patrolPoint;
  }

  private Vector3 PickPatrolCenter()
  {
    // Give each AI ship a home area somewhere on the map.
    float patrolExtent = AiShipWorldSettings.MapHalfExtent - AiShipWorldSettings.PatrolInset;
    float x = _rng.RandfRange(-patrolExtent, patrolExtent);
    float z = _rng.RandfRange(-patrolExtent, patrolExtent);
    return new Vector3(x, _spawnPoint.Y, z);
  }

  private Vector3 PickPatrolPointInRange()
  {
    // Pick a random wander target inside a circle around the patrol center.
    // Sqrt keeps the points spread across the full area instead of clustering
    // near the outer edge.
    float angle = _rng.RandfRange(0.0f, Mathf.Tau);
    float distance = Mathf.Sqrt(_rng.Randf()) * _definition.PatrolRadius;
    Vector3 offset = new(
      Mathf.Cos(angle) * distance,
      0.0f,
      Mathf.Sin(angle) * distance
    );

    Vector3 candidate = _patrolCenter + offset;
    float patrolExtent = AiShipWorldSettings.MapHalfExtent - AiShipWorldSettings.PatrolInset;

    return new Vector3(
      Mathf.Clamp(candidate.X, -patrolExtent, patrolExtent),
      _spawnPoint.Y,
      Mathf.Clamp(candidate.Z, -patrolExtent, patrolExtent)
    );
  }

  private void UpdateEscapeState(
    float delta,
    bool frontBlocked,
    bool leftBlocked,
    bool rightBlocked,
    bool isPatrolling)
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

    // If the ship was only roaming, don't send it right back toward the same
    // bad destination after it escapes.
    if (isPatrolling)
      _patrolPoint = PickPatrolPointInRange();
  }

  private void RefreshDebugVisuals()
  {
    if (!IsInsideTree())
      return;

    bool shouldShowDebug = _debugEnabled && Multiplayer.IsServer();

    if (!shouldShowDebug)
    {
      _patrolDebugMarker?.QueueFree();
      _patrolDebugMarker = null;
      _stateDebugLabel?.QueueFree();
      _stateDebugLabel = null;
      return;
    }

    if (_patrolDebugMarker == null)
    {
      _patrolDebugMarker = AiDebugVisuals.CreatePointMarker("PatrolDebugMarker", new Color(0.25f, 0.95f, 0.65f));
      GetTree().CurrentScene?.AddChild(_patrolDebugMarker);
    }

    if (_stateDebugLabel == null)
    {
      _stateDebugLabel = AiDebugVisuals.CreateStateLabel("AiStateDebugLabel", DebugLabelOffset);
      AddChild(_stateDebugLabel);
    }
  }

  private void UpdateDebugVisuals()
  {
    if (!_debugEnabled || !Multiplayer.IsServer())
      return;

    if (!IsInsideTree())
      return;

    RefreshDebugVisuals();

    if (_patrolDebugMarker != null)
      _patrolDebugMarker.GlobalPosition = _patrolCenter + DebugMarkerHeightOffset;

    if (_stateDebugLabel != null)
      _stateDebugLabel.Text = BuildDebugText();
  }

  private string BuildDebugText()
  {
    string targetMode = _debugHasTargetPlayer ? "Player" : "Patrol";
    string obstacleFlags = $"{(_debugFrontBlocked ? "F" : "-")}{(_debugLeftBlocked ? "L" : "-")}{(_debugRightBlocked ? "R" : "-")}";
    string escapeMode = _escapeTimerRemaining > 0.0f
      ? (_escapeTimerRemaining > EscapeForwardDurationSeconds ? "Reverse" : "Forward")
      : "None";

    return string.Join('\n', [
      DisplayName,
      $"State: {_debugState}",
      $"Mode: {targetMode}  Goal: {_debugDistanceToGoal:0.0}",
      $"Speed: {_currentSpeed:0.0}/{_definition.MaxSpeed:0.0}  Turn: {_currentTurnInput:0.00}",
      $"Patrol: ({_patrolCenter.X:0}, {_patrolCenter.Z:0}) r={_definition.PatrolRadius:0}",
      $"Wander: ({_patrolPoint.X:0}, {_patrolPoint.Z:0})",
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
