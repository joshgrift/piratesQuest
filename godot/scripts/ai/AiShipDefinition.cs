namespace PiratesQuest.AI;

using PiratesQuest.Data;
using PiratesQuest.AI.hunterDeterministic;
using PiratesQuest.AI.traderDeterministic;
using System.Collections.Generic;

/// <summary>
/// Data-only definition for one AI ship archetype.
/// 
/// Players use components and progression to reach their stats.
/// AI ships are simpler: we just declare the stats directly here.
/// </summary>
public sealed class AiShipDefinition
{
  public string Id { get; init; } = "raider";
  public string DisplayName { get; init; } = "Raider";
  public string AllyTypeId { get; init; } = "raider";
  public AiShipVisualType VisualType { get; init; } = AiShipVisualType.RaiderBlack;
  public float MaxHealth { get; init; } = 120.0f;
  public float MaxSpeed { get; init; } = 11.5f;
  public float Acceleration { get; init; } = 3.6f;
  public float Deceleration { get; init; } = 3.8f;
  public float TurnSpeed { get; init; } = 0.45f;
  public float AttackDamage { get; init; } = 20.0f;
  public float ProjectileBonusSpeed { get; init; } = 28.0f;
  public float FireCooldownSeconds { get; init; } = 2.5f;
  public float DetectionRange { get; init; } = 145.0f;
  public float ShipAvoidanceRange { get; init; } = 110.0f;
  public float PreferredCombatRange { get; init; } = 50.0f;
  public float FireRange { get; init; } = 70.0f;
  public float GoalArrivalDistance { get; init; } = 20.0f;
  public float PatrolRadius { get; init; } = 250.0f;
  public Dictionary<InventoryItemType, int> CargoManifest { get; init; } = [];

  public static AiShipDefinition FromId(string id)
  {
    // This switch is the place to add more AI archetypes later.
    return (id ?? string.Empty).Trim().ToLowerInvariant() switch
    {
      "raider" => CreateRaider(),
      "trader" => CreateTrader(),
      _ => CreateRaider(),
    };
  }

  public IAiShipController CreateController()
  {
    return Id switch
    {
      "trader" => new TraderDeterministicAiShipController(),
      _ => new HunterAiShipController(),
    };
  }

  private static AiShipDefinition CreateRaider()
  {
    return new AiShipDefinition
    {
      Id = "raider",
      DisplayName = "Raider",
      AllyTypeId = "raider",
      VisualType = AiShipVisualType.RaiderBlack,
      MaxHealth = 135.0f,
      MaxSpeed = 12.5f,
      Acceleration = 4.0f,
      Deceleration = 4.4f,
      TurnSpeed = 0.52f,
      AttackDamage = 18.0f,
      ProjectileBonusSpeed = 30.0f,
      FireCooldownSeconds = 2.2f,
      DetectionRange = 155.0f,
      ShipAvoidanceRange = 90.0f,
      PreferredCombatRange = 48.0f,
      FireRange = 72.0f,
      GoalArrivalDistance = 20.0f,
      PatrolRadius = 250.0f,
      CargoManifest = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Coin, 24 },
        { InventoryItemType.Wood, 18 },
        { InventoryItemType.CannonBall, 8 },
      }
    };
  }

  private static AiShipDefinition CreateTrader()
  {
    return new AiShipDefinition
    {
      Id = "trader",
      DisplayName = "Trader",
      AllyTypeId = "trader",
      VisualType = AiShipVisualType.PlayerWhite,
      MaxHealth = 110.0f,
      MaxSpeed = 13.0f,
      Acceleration = 4.2f,
      Deceleration = 4.6f,
      TurnSpeed = 0.56f,
      AttackDamage = 0.0f,
      ProjectileBonusSpeed = 0.0f,
      FireCooldownSeconds = 999.0f,
      DetectionRange = 0.0f,
      ShipAvoidanceRange = 150.0f,
      PreferredCombatRange = 0.0f,
      FireRange = 0.0f,
      GoalArrivalDistance = 32.0f,
      PatrolRadius = 0.0f,
      CargoManifest = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Coin, 30 },
        { InventoryItemType.Wood, 12 },
        { InventoryItemType.Iron, 10 },
        { InventoryItemType.Fish, 16 },
      }
    };
  }
}
