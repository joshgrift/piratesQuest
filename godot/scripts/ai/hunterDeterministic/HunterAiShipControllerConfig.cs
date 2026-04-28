namespace PiratesQuest.AI.hunterDeterministic;

/// <summary>
/// Static tuning values for the hunter controller.
///
/// These come from the AI ship definition once at controller creation time,
/// so we do not have to pass them again every physics frame.
/// </summary>
public sealed class HunterAiShipControllerConfig
{
  /// <summary>
  /// Range where hunter ships can start lining up a broadside.
  /// </summary>
  public float FireRange { get; init; }

  /// <summary>
  /// Distance the hunter tries to hold during combat.
  /// </summary>
  public float PreferredCombatRange { get; init; }

  /// <summary>
  /// Radius used when picking patrol points around the patrol center.
  /// </summary>
  public float PatrolRadius { get; init; }
}
