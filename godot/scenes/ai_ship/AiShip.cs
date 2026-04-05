namespace PiratesQuest;

using Godot;
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
  [Export] public Node3D VisualRoot { get; set; }

  [ExportGroup("Water Physics")]
  [Export] public NodePath WaterPlanePath { get; set; } = new("/root/Play/WaterPlane");
  [Export] public float ShipLength { get; set; } = 10.0f;
  [Export] public float VisualBobStrength { get; set; } = 0.35f;
  [Export] public float WaterSmoothSpeed { get; set; } = 7.0f;
  [Export] public bool ShowWaterDebug { get; set; } = false;

  public MultiplayerSpawner ProjectileSpawner { get; set; }
  public MultiplayerSpawner DeadPlayerSpawner { get; set; }
  public int Health { get; set; } = 100;
  public int MaxHealth => Mathf.RoundToInt(_definition.MaxHealth);

  private AiShipDefinition _definition = AiShipDefinition.FromId("raider");
  private IAiShipController _controller = new HunterAiShipController();
  private readonly RandomNumberGenerator _rng = new();
  private readonly Dictionary<InventoryItemType, int> _cargoManifest = [];
  private readonly List<Player> _nearbyPlayers = [];

  private Vector3 _spawnPoint;
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
  private bool _isSinking = false;
  private FloatingBody3D _floatingBody;

  private const float RecoilRollAmount = 0.32f;
  private const float RecoilDecaySpeed = 2.4f;

  public override void _Ready()
  {
    AddToGroup("ai_ships");

    _rng.Randomize();
    _lastPosition = GlobalPosition;

    _floatingBody = new FloatingBody3D(this);
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
    _controller = new HunterAiShipController();

    Name = data.ContainsKey("name") ? data["name"].AsString() : $"ai_ship_{GetInstanceId()}";
    DisplayName = data.ContainsKey("displayName") ? data["displayName"].AsString() : _definition.DisplayName;
    GlobalPosition = data.ContainsKey("position") ? data["position"].AsVector3() : Vector3.Zero;
    Rotation = data.ContainsKey("rotation") ? data["rotation"].AsVector3() : Vector3.Zero;

    _spawnPoint = GlobalPosition;
    _patrolPoint = PickPatrolPoint();
    Health = MaxHealth;

    _cargoManifest.Clear();
    foreach (var entry in _definition.CargoManifest)
      _cargoManifest[entry.Key] = entry.Value;
  }

  public override void _PhysicsProcess(double delta)
  {
    if (!IsMultiplayerAuthority() || _isSinking)
      return;

    if (_fireCooldownRemaining > 0.0f)
      _fireCooldownRemaining = Mathf.Max(0.0f, _fireCooldownRemaining - (float)delta);

    var targetPlayer = FindNearestTargetPlayer();
    Port nearestPort = FindNearestPort();
    Vector3 goalPosition = targetPlayer != null ? targetPlayer.GlobalPosition : GetPatrolPoint();

    if (targetPlayer == null && GlobalPosition.DistanceTo(_patrolPoint) < 14.0f)
      _patrolPoint = PickPatrolPoint();

    float traveled = GlobalPosition.DistanceTo(_lastPosition);
    _lastPosition = GlobalPosition;

    bool isTryingToMove = _currentSpeed > 2.5f;
    if (isTryingToMove && traveled < 0.05f)
      _stuckTimer += (float)delta;
    else
      _stuckTimer = 0.0f;

    Vector3 localGoal = ToLocal(goalPosition);
    var context = new AiShipContext
    {
      ShipPosition = GlobalPosition,
      CurrentSpeed = _currentSpeed,
      GoalPosition = goalPosition,
      LocalGoalPosition = localGoal,
      HasTargetPlayer = targetPlayer != null,
      DistanceToGoal = GlobalPosition.DistanceTo(goalPosition),
      FireRange = _definition.FireRange,
      PreferredCombatRange = _definition.PreferredCombatRange,
      FrontBlocked = ForwardRay?.IsColliding() ?? false,
      LeftBlocked = ForwardLeftRay?.IsColliding() ?? false,
      RightBlocked = ForwardRightRay?.IsColliding() ?? false,
      IsStuck = _stuckTimer >= 1.0f,
      NearestPort = nearestPort,
    };

    ApplyControl(_controller.GetControl(context, delta), (float)delta);
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

  private Vector3 GetPatrolPoint()
  {
    return _patrolPoint;
  }

  private Vector3 PickPatrolPoint()
  {
    // Small random sea patrol around the spawn point.
    // This keeps the first version lively without adding route data yet.
    float x = _rng.RandfRange(-_definition.PatrolRadius, _definition.PatrolRadius);
    float z = _rng.RandfRange(-_definition.PatrolRadius, _definition.PatrolRadius);
    return _spawnPoint + new Vector3(x, 0.0f, z);
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
}
