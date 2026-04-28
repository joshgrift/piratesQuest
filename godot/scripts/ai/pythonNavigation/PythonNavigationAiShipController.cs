namespace PiratesQuest.AI.pythonNavigation;

using Godot;
using System;

/// <summary>
/// First Python-backed AI archetype.
///
/// v1 is intentionally narrow:
/// - one waypoint goal at a time
/// - terrain-aware navigation only
/// - one shared Python worker process
/// - one sampled decision every 0.2 seconds
///
/// The ship scene still owns movement, collision, and escape state. This
/// controller just translates that scene snapshot into observations and rewards.
/// </summary>
public sealed class PythonNavigationAiShipController : IAiShipController
{
  private const float DecisionIntervalSeconds = 0.2f;
  private const string KeyShipId = "python_nav.ship_id";
  private const string KeyEpisodeId = "python_nav.episode_id";
  private const string KeyPatrolCenter = "python_nav.patrol_center";
  private const string KeyPatrolPoint = "python_nav.patrol_point";
  private const string KeyDecisionAccumulator = "python_nav.decision_accumulator";
  private const string KeyPendingSequence = "python_nav.pending_sequence";
  private const string KeyNextSequence = "python_nav.next_sequence";
  private const string KeyPendingObservation = "python_nav.pending_observation";
  private const string KeyLastObservation = "python_nav.last_observation";
  private const string KeyLastAction = "python_nav.last_action";
  private const string KeyLastDistanceToGoal = "python_nav.last_distance_to_goal";
  private const string KeySceneIsStuck = "python_nav.scene_is_stuck";
  private const string KeySceneIsEscaping = "python_nav.scene_is_escaping";
  private const string KeySceneIsEscapeReversing = "python_nav.scene_is_escape_reversing";
  private const string KeySceneEscapeTurnDirection = "python_nav.scene_escape_turn_direction";

  private readonly RandomNumberGenerator _rng = new();
  private readonly PythonAiWorkerClient _worker;
  private readonly float _patrolRadius;
  private readonly float _goalArrivalDistance;
  private readonly float _maxSpeed;

  public PythonNavigationAiShipController(
    PythonAiWorkerClient worker,
    float patrolRadius,
    float goalArrivalDistance,
    float maxSpeed)
  {
    _worker = worker;
    _patrolRadius = Mathf.Max(1.0f, patrolRadius);
    _goalArrivalDistance = Mathf.Max(1.0f, goalArrivalDistance);
    _maxSpeed = Mathf.Max(1.0f, maxSpeed);
    _rng.Randomize();
  }

  public void SyncSceneMemory(
    AiShipMemory memory,
    bool isStuck,
    bool isEscaping,
    bool isEscapeReversing,
    float escapeTurnDirection)
  {
    memory.Set(KeySceneIsStuck, isStuck);
    memory.Set(KeySceneIsEscaping, isEscaping);
    memory.Set(KeySceneIsEscapeReversing, isEscapeReversing);
    memory.Set(KeySceneEscapeTurnDirection, escapeTurnDirection);
  }

  public AiShipControlInput GetControl(AiShipContext context, AiShipMemory memory, double delta)
  {
    EnsureMemoryBootstrapped(memory, context);
    TryApplyPendingAction(memory);

    float accumulator = memory.GetOrDefault(KeyDecisionAccumulator, 0.0f) + (float)delta;
    while (accumulator >= DecisionIntervalSeconds)
    {
      accumulator -= DecisionIntervalSeconds;
      RunDecisionSample(context, memory);
      TryApplyPendingAction(memory);
    }
    memory.Set(KeyDecisionAccumulator, accumulator);

    if (memory.TryGet<PythonAiActionSnapshot>(KeyLastAction, out var action))
    {
      return new AiShipControlInput
      {
        Throttle = action.Throttle,
        Turn = action.Turn,
        FireLeft = false,
        FireRight = false,
        DebugState = string.IsNullOrWhiteSpace(action.DebugState) ? "Python Navigate" : action.DebugState
      };
    }

    return new AiShipControlInput
    {
      Throttle = 0.0f,
      Turn = 0.0f,
      FireLeft = false,
      FireRight = false,
      DebugState = "Waiting For Python"
    };
  }

  public void OnRemoved(AiShipMemory memory, string reason)
  {
    if (_worker == null || !_worker.IsAvailable)
      return;

    if (!memory.TryGet<string>(KeyShipId, out var shipId))
      return;
    if (!memory.TryGet<string>(KeyEpisodeId, out var episodeId))
      return;
    if (!memory.TryGet<PythonAiObservation>(KeyLastObservation, out var observation))
      return;
    if (!memory.TryGet<PythonAiActionSnapshot>(KeyLastAction, out var action))
      return;

    _worker.TrySendTransition(
      shipId,
      episodeId,
      observation,
      action,
      reward: 0.0f,
      nextObservation: observation,
      done: true,
      doneReason: reason);
  }

  private void EnsureMemoryBootstrapped(AiShipMemory memory, AiShipContext context)
  {
    memory.GetOrCreate(KeyShipId, () => Guid.NewGuid().ToString("N"));
    memory.GetOrCreate(KeyEpisodeId, () => Guid.NewGuid().ToString("N"));
    memory.GetOrCreate(KeyPatrolCenter, () => PickPatrolCenter(context));
    memory.GetOrCreate(KeyPatrolPoint, () => PickPatrolPoint(memory, context));
    memory.GetOrCreate(KeyLastDistanceToGoal, () => DistanceToPatrolPoint(memory, context.ShipPosition));
  }

  private void RunDecisionSample(AiShipContext context, AiShipMemory memory)
  {
    PythonAiObservation currentObservation = BuildObservation(context, memory);
    bool goalReached = DistanceToPatrolPoint(memory, context.ShipPosition) <= _goalArrivalDistance;

    if (memory.TryGet<PythonAiObservation>(KeyLastObservation, out var previousObservation) &&
        memory.TryGet<PythonAiActionSnapshot>(KeyLastAction, out var previousAction) &&
        memory.TryGet<string>(KeyShipId, out var shipId) &&
        memory.TryGet<string>(KeyEpisodeId, out var episodeId))
    {
      float reward = ComputeReward(memory, currentObservation, goalReached);
      _worker?.TrySendTransition(
        shipId,
        episodeId,
        previousObservation,
        previousAction,
        reward,
        currentObservation,
        done: goalReached,
        doneReason: goalReached ? "goal_reached" : string.Empty);
    }

    if (goalReached)
    {
      StartNextEpisode(memory, context);
      memory.Remove(KeyLastObservation);
      memory.Remove(KeyLastAction);
      memory.Remove(KeyPendingObservation);
      memory.Remove(KeyPendingSequence);
      currentObservation = BuildObservation(context, memory);
    }

    memory.Set(KeyLastDistanceToGoal, DistanceToPatrolPoint(memory, context.ShipPosition));
    RequestDecisionIfNeeded(memory, currentObservation);
  }

  private void RequestDecisionIfNeeded(AiShipMemory memory, PythonAiObservation observation)
  {
    if (_worker == null || !_worker.IsAvailable)
      return;

    if (memory.ContainsKey(KeyPendingSequence))
      return;
    if (!memory.TryGet<string>(KeyShipId, out var shipId))
      return;
    if (!memory.TryGet<string>(KeyEpisodeId, out var episodeId))
      return;

    int sequence = memory.GetOrDefault(KeyNextSequence, 1);
    if (!_worker.TryRequestDecision(shipId, sequence, episodeId, observation))
      return;

    memory.Set(KeyPendingSequence, sequence);
    memory.Set(KeyNextSequence, sequence + 1);
    memory.Set(KeyPendingObservation, observation);
  }

  private void TryApplyPendingAction(AiShipMemory memory)
  {
    if (_worker == null || !_worker.IsAvailable)
      return;

    if (!memory.TryGet<string>(KeyShipId, out var shipId))
      return;
    if (!memory.TryGet<int>(KeyPendingSequence, out var pendingSequence))
      return;
    if (!_worker.TryTakeAction(shipId, pendingSequence, out var actionResult))
      return;

    memory.Set(KeyLastAction, new PythonAiActionSnapshot
    {
      Throttle = actionResult.Throttle,
      Turn = actionResult.Turn,
      DebugState = actionResult.DebugState
    });

    if (memory.TryGet<PythonAiObservation>(KeyPendingObservation, out var pendingObservation))
      memory.Set(KeyLastObservation, pendingObservation);

    memory.Remove(KeyPendingObservation);
    memory.Remove(KeyPendingSequence);
  }

  private void StartNextEpisode(AiShipMemory memory, AiShipContext context)
  {
    memory.Set(KeyEpisodeId, Guid.NewGuid().ToString("N"));
    memory.Set(KeyPatrolPoint, PickPatrolPoint(memory, context));
    memory.Set(KeyDecisionAccumulator, 0.0f);
  }

  private Vector3 PickPatrolCenter(AiShipContext context)
  {
    float patrolExtent = AiShipWorldSettings.MapHalfExtent - AiShipWorldSettings.PatrolInset;
    float x = _rng.RandfRange(-patrolExtent, patrolExtent);
    float z = _rng.RandfRange(-patrolExtent, patrolExtent);
    return new Vector3(x, context.SpawnPoint.Y, z);
  }

  private Vector3 PickPatrolPoint(AiShipMemory memory, AiShipContext context)
  {
    Vector3 center = memory.GetOrDefault(KeyPatrolCenter, context.ShipPosition);
    float angle = _rng.RandfRange(0.0f, Mathf.Tau);
    float distance = Mathf.Sqrt(_rng.Randf()) * _patrolRadius;
    Vector3 offset = new(
      Mathf.Cos(angle) * distance,
      0.0f,
      Mathf.Sin(angle) * distance
    );

    Vector3 candidate = center + offset;
    float patrolExtent = AiShipWorldSettings.MapHalfExtent - AiShipWorldSettings.PatrolInset;

    return new Vector3(
      Mathf.Clamp(candidate.X, -patrolExtent, patrolExtent),
      context.SpawnPoint.Y,
      Mathf.Clamp(candidate.Z, -patrolExtent, patrolExtent)
    );
  }

  private PythonAiObservation BuildObservation(AiShipContext context, AiShipMemory memory)
  {
    Vector3 patrolPoint = memory.GetOrDefault(KeyPatrolPoint, context.ShipPosition);
    Vector3 localGoal = context.ShipBasis.Inverse() * (patrolPoint - context.ShipPosition);
    float distanceToGoal = context.ShipPosition.DistanceTo(patrolPoint);

    return new PythonAiObservation
    {
      GoalLocalX = Mathf.Clamp(localGoal.X / _patrolRadius, -1.0f, 1.0f),
      GoalLocalZ = Mathf.Clamp(localGoal.Z / _patrolRadius, -1.0f, 1.0f),
      DistanceToGoal = Mathf.Clamp(distanceToGoal / _patrolRadius, 0.0f, 2.0f),
      SpeedFraction = Mathf.Clamp(context.CurrentSpeed / _maxSpeed, 0.0f, 1.0f),
      ForwardDistanceFraction = GetRayDistanceFraction(context, AiShipRayIds.Forward),
      ForwardLeftDistanceFraction = GetRayDistanceFraction(context, AiShipRayIds.ForwardLeft),
      ForwardRightDistanceFraction = GetRayDistanceFraction(context, AiShipRayIds.ForwardRight),
      WideLeftDistanceFraction = GetRayDistanceFraction(context, AiShipRayIds.WideLeft),
      WideRightDistanceFraction = GetRayDistanceFraction(context, AiShipRayIds.WideRight),
      LeftPressure = Mathf.Clamp(AiNavigationHelpers.GetLeftObstacleStrength(context) / 1.75f, 0.0f, 1.0f),
      RightPressure = Mathf.Clamp(AiNavigationHelpers.GetRightObstacleStrength(context) / 1.75f, 0.0f, 1.0f),
      FrontBlocked = AiNavigationHelpers.IsFrontBlocked(context),
      IsStuck = memory.GetOrDefault(KeySceneIsStuck, false),
      IsEscaping = memory.GetOrDefault(KeySceneIsEscaping, false)
    };
  }

  private float ComputeReward(AiShipMemory memory, PythonAiObservation currentObservation, bool goalReached)
  {
    float previousDistance = memory.GetOrDefault(KeyLastDistanceToGoal, currentObservation.DistanceToGoal * _patrolRadius);
    float currentDistance = currentObservation.DistanceToGoal * _patrolRadius;
    float reward = previousDistance - currentDistance;

    if (currentObservation.FrontBlocked)
      reward -= 0.05f;

    if (currentObservation.IsStuck || currentObservation.IsEscaping)
      reward -= 0.10f;

    if (goalReached)
      reward += 1.0f;

    return reward;
  }

  private static float GetRayDistanceFraction(AiShipContext context, string rayId)
  {
    if (!context.TryGetTerrainRay(rayId, out AiShipTerrainRay ray))
      return 1.0f;

    if (ray.MaxDistance <= 0.001f)
      return 1.0f;

    return Mathf.Clamp(ray.Distance / ray.MaxDistance, 0.0f, 1.0f);
  }

  private float DistanceToPatrolPoint(AiShipMemory memory, Vector3 shipPosition)
  {
    Vector3 patrolPoint = memory.GetOrDefault(KeyPatrolPoint, shipPosition);
    return shipPosition.DistanceTo(patrolPoint);
  }
}
