namespace PiratesQuest.AI.traderDeterministic;

/// <summary>
/// Static tuning values for the trader controller.
///
/// These come from the AI ship definition once at controller creation time,
/// so we do not have to pass them again every physics frame.
/// </summary>
public sealed class TraderDeterministicAiShipControllerConfig
{
  /// <summary>
  /// Distance where a trader considers a port "reached" and chooses the next one.
  /// </summary>
  public float GoalArrivalDistance { get; init; }
}
