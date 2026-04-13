namespace PiratesQuest.AI;

/// <summary>
/// Decision-only interface for AI ships.
/// 
/// The controller decides what the ship wants to do.
/// The AiShip node is still responsible for movement, combat, and loot.
/// That split keeps the gameplay rules in one place.
/// </summary>
public interface IAiShipController
{
  AiShipControlInput GetControl(AiShipContext context, double delta);
}

/// <summary>
/// The AI brain produces one of these each physics frame.
/// 
/// It is intentionally tiny:
/// - Throttle controls forward/backward movement
/// - Turn controls steering
/// - FireLeft / FireRight request a broadside
/// 
/// Keeping this small makes it easy to swap different AI approaches later.
/// </summary>
public sealed class AiShipControlInput
{
  /// <summary>
  /// Forward/backward throttle request for this frame.
  /// 
  /// Typical values:
  /// - 1 = full forward
  /// - 0 = coast / stop accelerating
  /// - -1 = full reverse
  /// 
  /// The ship scene converts this into real speed using acceleration and
  /// deceleration from the active AI ship definition.
  /// </summary>
  public float Throttle { get; set; }

  /// <summary>
  /// Steering request for this frame.
  /// 
  /// Typical values:
  /// - -1 = hard port/left
  /// - 0 = no turn
  /// - 1 = hard starboard/right
  /// 
  /// The ship scene applies this using the definition's turn speed, so the AI
  /// asks for intent here, not exact degrees to rotate.
  /// </summary>
  public float Turn { get; set; }

  /// <summary>
  /// Request to fire the left broadside this frame.
  /// The ship scene still decides whether firing is actually allowed based on cooldowns.
  /// Trader AI should leave this false.
  /// </summary>
  public bool FireLeft { get; set; }

  /// <summary>
  /// Request to fire the right broadside this frame.
  /// The ship scene still checks gameplay rules like cooldown and cannon pivots.
  /// Trader AI should leave this false.
  /// </summary>
  public bool FireRight { get; set; }

  /// <summary>
  /// Human-readable description of what the AI thinks it is doing right now.
  /// This is only for debugging and labels, so changing it does not affect behavior.
  /// Good examples: "Patrol", "Avoid Terrain", "Broadside Setup", "Avoid Ship".
  /// </summary>
  public string DebugState { get; set; } = string.Empty;
}
