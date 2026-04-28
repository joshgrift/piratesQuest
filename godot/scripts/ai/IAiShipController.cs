namespace PiratesQuest.AI;

/// <summary>
/// Decision-only interface for AI ships.
///
/// The controller decides what the ship wants to do.
/// The AiShip node is still responsible for movement, combat, and loot.
/// The controller also gets a small memory bag so each AI can remember
/// per-ship runtime state without pushing that state into AiShip.cs.
/// </summary>
public interface IAiShipController
{
  /// <summary>
  /// Let the scene share its current short-lived recovery state with the AI.
  ///
  /// Each AI controller decides how to store these values in its own memory.
  /// That keeps memory keys controller-specific instead of making them global.
  /// </summary>
  void SyncSceneMemory(
    AiShipMemory memory,
    bool isStuck,
    bool isEscaping,
    bool isEscapeReversing,
    float escapeTurnDirection);

  AiShipControlInput GetControl(AiShipContext context, AiShipMemory memory, double delta);

  /// <summary>
  /// Called exactly once before the ship leaves the scene (sink, forced respawn,
  /// worker failure, or scene shutdown). Lets the controller flush any per-ship
  /// state it owns, such as closing an episode or notifying a worker process.
  /// </summary>
  void OnRemoved(AiShipMemory memory, string reason);
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
