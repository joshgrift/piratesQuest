namespace PiratesQuest.AI.hunterDeterministic;

using Godot;
using PiratesQuest.AI;
using System;

/// <summary>
/// First AI brain: patrol open water, chase nearby players, and try to line up
/// broadsides instead of simply ramming straight at the target.
/// </summary>
public sealed class HunterAiShipController : IAiShipController
{
  private const string PatrolCenterKey = "hunter.patrol_center";
  private const string PatrolPointKey = "hunter.patrol_point";
  private const string WasEscapingKey = "hunter.was_escaping";
  private const string IsStuckKey = "hunter.is_stuck";
  private const string IsEscapingKey = "hunter.is_escaping";
  private const string IsEscapeReversingKey = "hunter.is_escape_reversing";
  private const string EscapeTurnDirectionKey = "hunter.escape_turn_direction";
  private const float PatrolArrivalDistance = 14.0f;

  private readonly HunterAiShipControllerConfig _config;
  private readonly RandomNumberGenerator _rng = new();

  public HunterAiShipController(HunterAiShipControllerConfig config)
  {
    ArgumentNullException.ThrowIfNull(config);
    _config = config;
    _rng.Randomize();
  }

  public void SyncSceneMemory(
    AiShipMemory memory,
    bool isStuck,
    bool isEscaping,
    bool isEscapeReversing,
    float escapeTurnDirection)
  {
    memory.Set(IsStuckKey, isStuck);
    memory.Set(IsEscapingKey, isEscaping);
    memory.Set(IsEscapeReversingKey, isEscapeReversing);
    memory.Set(EscapeTurnDirectionKey, escapeTurnDirection);
  }

  public AiShipControlInput GetControl(AiShipContext context, AiShipMemory memory, double delta)
  {
    var input = new AiShipControlInput();
    bool isStuck = GetIsStuck(memory);
    bool isEscaping = GetIsEscaping(memory);
    bool isEscapeReversing = GetIsEscapeReversing(memory);
    float escapeTurnDirection = GetEscapeTurnDirection(memory);
    AiShipContact targetShip = context.FindNearestHostileShip();
    float obstacleTurnBias = AiNavigationHelpers.BuildObstacleTurnBias(context);
    bool sideTerrainNearby = AiNavigationHelpers.HasSideTerrainNearby(context);
    bool wasEscaping = memory.TryGet<bool>(WasEscapingKey, out bool storedWasEscaping) && storedWasEscaping;

    AiShipControlInput FinishInput()
    {
      memory.Set(WasEscapingKey, isEscaping);
      return input;
    }

    // Once the ship commits to an escape maneuver, keep that decision stable
    // for a short time. This avoids the left/right wiggle that happens when
    // obstacle checks flip every frame near shore.
    if (isEscaping)
    {
      // If patrol mode just triggered an escape, pick a fresh patrol point now
      // so the ship does not head straight back into the same bad water after recovery.
      if (!wasEscaping && targetShip == null)
      {
        Vector3 patrolCenter = GetPatrolCenter(context, memory);
        memory.Set(PatrolPointKey, PickPatrolPointInRange(context, patrolCenter));
      }

      input.Throttle = isEscapeReversing ? -1.0f : 0.55f;
      input.Turn = escapeTurnDirection;
      input.DebugState = isEscapeReversing ? "Escape Reverse" : "Escape Forward";
      return FinishInput();
    }

    // Terrain avoidance gets top priority.
    // If the ship sees land dead ahead, surviving matters more than style.
    if (AiNavigationHelpers.IsFrontBlocked(context))
    {
      input.Throttle = -0.75f;
      input.Turn = AiNavigationHelpers.PickSaferTurn(context);
      input.DebugState = "Avoid Shore";
      return FinishInput();
    }

    // If we're already rubbing against terrain, reverse and pivot away.
    if (isStuck)
    {
      input.Throttle = -0.9f;
      input.Turn = AiNavigationHelpers.PickSaferTurn(context);
      input.DebugState = "Recover Stuck";
      return FinishInput();
    }

    if (targetShip == null)
    {
      Vector3 patrolCenter = GetPatrolCenter(context, memory);
      Vector3 patrolPoint = memory.GetOrCreate(PatrolPointKey, () => PickPatrolPointInRange(context, patrolCenter));
      float patrolDistance = context.ShipPosition.DistanceTo(patrolPoint);

      if (patrolDistance < PatrolArrivalDistance)
      {
        patrolPoint = PickPatrolPointInRange(context, patrolCenter);
        memory.Set(PatrolPointKey, patrolPoint);
        patrolDistance = context.ShipPosition.DistanceTo(patrolPoint);
      }

      Vector3 localPatrolPoint = context.ShipBasis.Inverse() * (patrolPoint - context.ShipPosition);
      float patrolTargetAngle = Mathf.Atan2(localPatrolPoint.X, -localPatrolPoint.Z);

      input.Throttle = patrolDistance > 12.0f ? 0.75f : 0.2f;
      float patrolTurn = Mathf.Clamp(patrolTargetAngle / 0.8f, -1.0f, 1.0f);
      float obstacleAssist = sideTerrainNearby ? obstacleTurnBias * 0.12f : 0.0f;
      input.Turn = Mathf.Clamp(patrolTurn + obstacleAssist, -1.0f, 1.0f);
      input.DebugState = "Patrol";
      return FinishInput();
    }

    Vector3 localTargetShipPosition = context.ShipBasis.Inverse() * (targetShip.Position - context.ShipPosition);
    float targetShipAngle = Mathf.Atan2(localTargetShipPosition.X, -localTargetShipPosition.Z);
    float targetShipDistance = targetShip.Distance;

    // When attacking, try to keep the target ship off one side of the hull so the
    // cannons have a clear broadside instead of staring at the bow.
    float desiredBroadsideAngle = localTargetShipPosition.X >= 0.0f
      ? Mathf.DegToRad(78.0f)
      : Mathf.DegToRad(-78.0f);

    bool inFireRange = targetShipDistance <= _config.FireRange;
    float steeringAngle = inFireRange
      ? AiNavigationHelpers.NormalizeAngle(targetShipAngle - desiredBroadsideAngle)
      : targetShipAngle;

    float chaseTurn = Mathf.Clamp(steeringAngle / 0.85f, -1.0f, 1.0f);
    float chaseObstacleAssist = sideTerrainNearby ? obstacleTurnBias * 0.1f : 0.0f;
    input.Turn = Mathf.Clamp(chaseTurn + chaseObstacleAssist, -1.0f, 1.0f);

    if (targetShipDistance > _config.PreferredCombatRange + 12.0f)
    {
      input.Throttle = 1.0f;
    }
    else if (targetShipDistance < _config.PreferredCombatRange * 0.65f)
    {
      input.Throttle = 0.2f;
    }
    else
    {
      input.Throttle = 0.65f;
    }

    if (inFireRange && Mathf.Abs(steeringAngle) < 0.22f)
    {
      input.FireRight = localTargetShipPosition.X > 0.0f;
      input.FireLeft = localTargetShipPosition.X < 0.0f;
      input.DebugState = input.FireRight ? "Fire Starboard" : "Fire Port";
      return FinishInput();
    }

    input.DebugState = inFireRange ? "Broadside Setup" : "Chase";
    return FinishInput();
  }

  private Vector3 GetPatrolCenter(AiShipContext context, AiShipMemory memory)
  {
    return memory.GetOrCreate(PatrolCenterKey, () => PickPatrolCenter(context));
  }

  private static bool GetIsStuck(AiShipMemory memory)
  {
    return memory.TryGet(IsStuckKey, out bool value) && value;
  }

  private static bool GetIsEscaping(AiShipMemory memory)
  {
    return memory.TryGet(IsEscapingKey, out bool value) && value;
  }

  private static bool GetIsEscapeReversing(AiShipMemory memory)
  {
    return memory.TryGet(IsEscapeReversingKey, out bool value) && value;
  }

  private static float GetEscapeTurnDirection(AiShipMemory memory)
  {
    return memory.TryGet(EscapeTurnDirectionKey, out float value) ? value : 1.0f;
  }

  private Vector3 PickPatrolCenter(AiShipContext context)
  {
    float patrolExtent = AiShipWorldSettings.MapHalfExtent - AiShipWorldSettings.PatrolInset;
    float x = _rng.RandfRange(-patrolExtent, patrolExtent);
    float z = _rng.RandfRange(-patrolExtent, patrolExtent);
    return new Vector3(x, context.SpawnPoint.Y, z);
  }

  private Vector3 PickPatrolPointInRange(AiShipContext context, Vector3 patrolCenter)
  {
    if (_config.PatrolRadius <= 0.0f)
      return patrolCenter;

    // Sqrt spreads points across the whole patrol circle instead of stacking
    // them too heavily near the edge.
    float angle = _rng.RandfRange(0.0f, Mathf.Tau);
    float distance = Mathf.Sqrt(_rng.Randf()) * _config.PatrolRadius;
    Vector3 offset = new(
      Mathf.Cos(angle) * distance,
      0.0f,
      Mathf.Sin(angle) * distance
    );

    Vector3 candidate = patrolCenter + offset;
    float patrolExtent = AiShipWorldSettings.MapHalfExtent - AiShipWorldSettings.PatrolInset;

    return new Vector3(
      Mathf.Clamp(candidate.X, -patrolExtent, patrolExtent),
      context.SpawnPoint.Y,
      Mathf.Clamp(candidate.Z, -patrolExtent, patrolExtent)
    );
  }

  public void OnRemoved(AiShipMemory memory, string reason)
  {
  }
}
