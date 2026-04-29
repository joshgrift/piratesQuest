namespace PiratesQuest.AI.pythonNavigation;

using Godot;
using System;

/// <summary>
/// First Python-backed AI archetype.
///
/// v1 is intentionally narrow:
/// - one target port at a time
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
  private const string KeyTargetPortId = "python_nav.target_port_id";
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

  private readonly PythonAiWorkerClient _worker;
  private readonly float _goalArrivalDistance;
  private readonly float _maxSpeed;
  private readonly RandomNumberGenerator _rng = new();

  public PythonNavigationAiShipController(
    PythonAiWorkerClient worker,
    float _unusedPatrolRadius,
    float goalArrivalDistance,
    float maxSpeed)
  {
    // Keep the existing constructor shape so the archetype factory does not need
    // a special case for this controller. We do not use patrol radius anymore
    // because the RL agent now navigates between ports instead of local waypoints.
    _worker = worker;
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
        DebugState = string.IsNullOrWhiteSpace(action.DebugState) ? "Python Port RL" : action.DebugState
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
    memory.GetOrCreate(KeyTargetPortId, () => PickTargetPortId(context, excludedPortId: string.Empty));
    memory.GetOrCreate(KeyLastDistanceToGoal, () => DistanceToTargetPort(memory, context));
  }

  private void RunDecisionSample(AiShipContext context, AiShipMemory memory)
  {
    Port targetPort = ResolveTargetPort(memory, context);
    if (targetPort == null)
      return;

    PythonAiObservation currentObservation = BuildObservation(context, memory, targetPort);
    bool portTouched = HasReachedTargetPort(targetPort, context.ShipPosition);

    if (memory.TryGet<PythonAiObservation>(KeyLastObservation, out var previousObservation) &&
        memory.TryGet<PythonAiActionSnapshot>(KeyLastAction, out var previousAction) &&
        memory.TryGet<string>(KeyShipId, out var shipId) &&
        memory.TryGet<string>(KeyEpisodeId, out var episodeId))
    {
      float reward = ComputeReward(memory, currentObservation, portTouched);
      _worker?.TrySendTransition(
        shipId,
        episodeId,
        previousObservation,
        previousAction,
        reward,
        currentObservation,
        done: portTouched,
        doneReason: portTouched ? "port_touched" : string.Empty);
    }

    if (portTouched)
    {
      StartNextEpisode(memory, context);
      memory.Remove(KeyLastObservation);
      memory.Remove(KeyLastAction);
      memory.Remove(KeyPendingObservation);
      memory.Remove(KeyPendingSequence);

      targetPort = ResolveTargetPort(memory, context);
      if (targetPort == null)
        return;

      currentObservation = BuildObservation(context, memory, targetPort);
    }

    memory.Set(KeyLastDistanceToGoal, DistanceToTargetPort(memory, context));
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
    string previousPortId = memory.GetOrDefault(KeyTargetPortId, string.Empty);
    memory.Set(KeyEpisodeId, Guid.NewGuid().ToString("N"));
    memory.Set(KeyTargetPortId, PickTargetPortId(context, previousPortId));
    memory.Set(KeyDecisionAccumulator, 0.0f);
  }

  private PythonAiObservation BuildObservation(AiShipContext context, AiShipMemory memory, Port targetPort)
  {
    Vector3 targetPosition = targetPort?.GlobalPosition ?? context.ShipPosition;
    Vector3 localGoal = context.ShipBasis.Inverse() * (targetPosition - context.ShipPosition);
    float maxGoalDistance = AiShipWorldSettings.MapHalfExtent * 2.0f;
    float distanceToGoal = context.ShipPosition.DistanceTo(targetPosition);

    return new PythonAiObservation
    {
      GoalLocalX = Mathf.Clamp(localGoal.X / maxGoalDistance, -1.0f, 1.0f),
      GoalLocalZ = Mathf.Clamp(localGoal.Z / maxGoalDistance, -1.0f, 1.0f),
      DistanceToGoal = Mathf.Clamp(distanceToGoal / maxGoalDistance, 0.0f, 1.0f),
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

  private float ComputeReward(AiShipMemory memory, PythonAiObservation currentObservation, bool portTouched)
  {
    float maxGoalDistance = AiShipWorldSettings.MapHalfExtent * 2.0f;
    float previousDistance = memory.GetOrDefault(KeyLastDistanceToGoal, currentObservation.DistanceToGoal * maxGoalDistance);
    float currentDistance = currentObservation.DistanceToGoal * maxGoalDistance;
    float reward = previousDistance - currentDistance;

    if (currentObservation.FrontBlocked)
      reward -= 0.05f;

    if (currentObservation.IsStuck || currentObservation.IsEscaping)
      reward -= 0.10f;

    if (portTouched)
      reward += 1.0f;

    return reward;
  }

  private Port ResolveTargetPort(AiShipMemory memory, AiShipContext context)
  {
    string targetPortId = memory.GetOrDefault(KeyTargetPortId, string.Empty);
    foreach (Port port in context.Ports)
    {
      if (port?.PortId == targetPortId)
        return port;
    }

    string replacementPortId = PickTargetPortId(context, excludedPortId: string.Empty);
    if (string.IsNullOrWhiteSpace(replacementPortId))
      return null;

    memory.Set(KeyTargetPortId, replacementPortId);
    foreach (Port port in context.Ports)
    {
      if (port?.PortId == replacementPortId)
        return port;
    }

    return null;
  }

  private string PickTargetPortId(AiShipContext context, string excludedPortId)
  {
    if (context.Ports.Length == 0)
      return string.Empty;

    var candidates = new Godot.Collections.Array<Port>();
    foreach (Port port in context.Ports)
    {
      if (port == null)
        continue;
      if (!string.IsNullOrWhiteSpace(excludedPortId) && port.PortId == excludedPortId && context.Ports.Length > 1)
        continue;

      candidates.Add(port);
    }

    if (candidates.Count == 0)
      return context.Ports[0]?.PortId ?? string.Empty;

    int index = _rng.RandiRange(0, candidates.Count - 1);
    return candidates[index]?.PortId ?? string.Empty;
  }

  private bool HasReachedTargetPort(Port targetPort, Vector3 shipPosition)
  {
    if (targetPort == null)
      return false;

    float reachDistance = Mathf.Max(_goalArrivalDistance, targetPort.InteractionRadius);
    return shipPosition.DistanceTo(targetPort.GlobalPosition) <= reachDistance;
  }

  private static float GetRayDistanceFraction(AiShipContext context, string rayId)
  {
    if (!context.TryGetTerrainRay(rayId, out AiShipTerrainRay ray))
      return 1.0f;

    if (ray.MaxDistance <= 0.001f)
      return 1.0f;

    return Mathf.Clamp(ray.Distance / ray.MaxDistance, 0.0f, 1.0f);
  }

  private float DistanceToTargetPort(AiShipMemory memory, AiShipContext context)
  {
    Port targetPort = ResolveTargetPort(memory, context);
    return targetPort == null
      ? 0.0f
      : context.ShipPosition.DistanceTo(targetPort.GlobalPosition);
  }
}
