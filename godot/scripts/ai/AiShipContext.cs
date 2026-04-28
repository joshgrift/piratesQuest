namespace PiratesQuest.AI;

using Godot;

/// <summary>
/// Read-only snapshot of the world as seen by an AI ship this frame.
/// 
/// We include stable ship/world state plus the current frame's nearby ship
/// contacts and terrain ray readings.
/// Static tuning values should live in controller config instead.
/// Cross-frame runtime state should live in AiShipMemory instead.
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
  /// Original spawn point for this AI ship.
  /// Controllers can use this as a stable reference when building patrol areas
  /// or other long-lived local behavior.
  /// </summary>
  public Vector3 SpawnPoint { get; init; }

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
  /// Nearby ships inside the global AI discovery range.
  /// This includes players and AI ships, and marks whether each contact is
  /// allied and whether it currently counts as a threat.
  /// </summary>
  public AiShipContact[] NearbyShips { get; init; } = [];

  /// <summary>
  /// Raw terrain ray readings for this frame.
  /// We use an array instead of hard-coded properties so the sensing setup can
  /// grow later without reshaping the context again.
  /// </summary>
  public AiShipTerrainRay[] TerrainRays { get; init; } = [];
}
