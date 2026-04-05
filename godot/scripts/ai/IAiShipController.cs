namespace PiratesQuest;

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
