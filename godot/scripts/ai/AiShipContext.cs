namespace PiratesQuest.AI;

using Godot;

/// <summary>
/// Read-only snapshot of the world as seen by an AI ship this frame.
/// 
/// We only include things AI ships actually care about right now:
/// nearby players, nearby ports, and obstacle sensing.
/// Any controller-owned goals should live in AiShipMemory instead.
/// </summary>
public sealed class AiShipContext
{
  /// <summary>
  /// The ship's current world position.
  /// AI uses this for distance checks, like "am I close enough to the goal?"
  /// or "how far away is the threat ship?"
  /// </summary>
  public Vector3 ShipPosition { get; init; }

  /// <summary>
  /// The ship's current world rotation basis.
  /// This lets an AI convert world-space directions into "my local left/right/front"
  /// directions when it wants to steer relative to the hull.
  /// </summary>
  public Basis ShipBasis { get; init; }

  /// <summary>
  /// Current forward speed in world units per second.
  /// AI can use this to decide whether it is moving well, slowing down too much,
  /// or failing to make progress.
  /// </summary>
  public float CurrentSpeed { get; init; }

  /// <summary>
  /// True when the ship scene found a player target for this frame.
  /// Hunter AI uses this to switch between patrol behavior and combat behavior.
  /// Traders usually ignore it and rely on threat-avoidance fields instead.
  /// </summary>
  public bool HasTargetPlayer { get; init; }

  /// <summary>
  /// World-space position of the player target, when one exists.
  /// If <see cref="HasTargetPlayer"/> is false this will be Vector3.Zero.
  /// </summary>
  public Vector3 TargetPlayerPosition { get; init; }

  /// <summary>
  /// Target player position already converted into the ship's local space.
  /// This makes it easy to ask "is the player to my left or right?" without
  /// each controller repeating the same transform work.
  /// </summary>
  public Vector3 LocalTargetPlayerPosition { get; init; }

  /// <summary>
  /// Straight-line distance to the current player target.
  /// If no player target exists this should be float.MaxValue.
  /// </summary>
  public float DistanceToTargetPlayer { get; init; }

  /// <summary>
  /// Original spawn point for this AI ship.
  /// Controllers can use this as a stable reference when building patrol areas
  /// or other long-lived local behavior.
  /// </summary>
  public Vector3 SpawnPoint { get; init; }

  /// <summary>
  /// Patrol radius from the active AI ship definition.
  /// Hunters use this to choose wander points inside their patrol area.
  /// </summary>
  public float PatrolRadius { get; init; }

  /// <summary>
  /// Distance where a broadside shot is allowed to start making sense.
  /// This comes from the active AI ship definition, so different archetypes can
  /// have different effective combat envelopes.
  /// </summary>
  public float FireRange { get; init; }

  /// <summary>
  /// The "sweet spot" combat distance the ship prefers to hold.
  /// Hunter AI uses this to avoid ramming too close or drifting too far away.
  /// Non-combat AI can usually ignore it.
  /// </summary>
  public float PreferredCombatRange { get; init; }

  /// <summary>
  /// How close the ship should get before the goal counts as reached.
  /// Traders use this for port arrival. Other AI could reuse it for future goal types.
  /// </summary>
  public float GoalArrivalDistance { get; init; }

  /// <summary>
  /// True when the center forward ray sees blocking terrain ahead.
  /// This is the strongest "danger now" terrain signal and usually takes top priority.
  /// </summary>
  public bool FrontBlocked { get; init; }

  /// <summary>
  /// True when terrain is detected on the ship's left side.
  /// AI uses this with <see cref="RightBlocked"/> to choose a safer turn direction.
  /// </summary>
  public bool LeftBlocked { get; init; }

  /// <summary>
  /// True when terrain is detected on the ship's right side.
  /// AI uses this with <see cref="LeftBlocked"/> to choose a safer turn direction.
  /// </summary>
  public bool RightBlocked { get; init; }

  /// <summary>
  /// Weighted score for how dangerous the left side currently looks.
  /// This is more detailed than a simple bool because multiple rays contribute.
  /// A higher number means "turning left is probably a worse idea."
  /// </summary>
  public float LeftObstacleStrength { get; init; }

  /// <summary>
  /// Weighted score for how dangerous the right side currently looks.
  /// Comparing this against <see cref="LeftObstacleStrength"/> gives the AI a
  /// more stable tie-breaker than just blocked/not blocked.
  /// </summary>
  public float RightObstacleStrength { get; init; }

  /// <summary>
  /// True when the ship appears to be trying to move but is not making progress.
  /// AI should usually reverse or perform an escape maneuver when this happens.
  /// </summary>
  public bool IsStuck { get; init; }

  /// <summary>
  /// True while the ship scene has already committed to a short escape sequence.
  /// When this is true, most AI should keep following the escape instead of
  /// rethinking the entire plan every frame.
  /// </summary>
  public bool IsEscaping { get; init; }

  /// <summary>
  /// True during the reversing half of the escape sequence.
  /// This helps the AI pick a strong reverse throttle before switching back to forward thrust.
  /// </summary>
  public bool IsEscapeReversing { get; init; }

  /// <summary>
  /// The direction the ship scene wants the AI to keep turning during escape.
  /// Usually -1 means turn left and 1 means turn right.
  /// The scene computes this once so the AI can stay stable during recovery.
  /// </summary>
  public float EscapeTurnDirection { get; init; }

  /// <summary>
  /// Closest port in the world.
  /// Helpful for simple awareness or debugging, even if a specific AI chooses
  /// a different port from the full <see cref="Ports"/> list.
  /// </summary>
  public Port NearestPort { get; init; }

  /// <summary>
  /// All ports currently known in the scene.
  /// Trader AI uses this to pick random destinations and cycle between ports.
  /// </summary>
  public Port[] Ports { get; init; } = [];

  /// <summary>
  /// True when a nearby non-allied ship has been found inside the avoidance range.
  /// Traders use this to switch from travel mode into flee mode.
  /// </summary>
  public bool HasNearbyThreatShip { get; init; }

  /// <summary>
  /// World-space position of the nearby threat ship.
  /// Useful for pure distance checks or debugging labels.
  /// </summary>
  public Vector3 NearbyThreatShipPosition { get; init; }

  /// <summary>
  /// Threat ship position converted into the ship's local space.
  /// This is the easiest way to decide which way to turn to run away:
  /// negate it, then steer away from that local direction.
  /// </summary>
  public Vector3 LocalNearbyThreatShipPosition { get; init; }

  /// <summary>
  /// Straight-line distance to the nearby threat ship.
  /// AI can use this to flee harder when danger is close and relax when it is farther away.
  /// </summary>
  public float DistanceToNearbyThreatShip { get; init; }
}
