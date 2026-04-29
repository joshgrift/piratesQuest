namespace PiratesQuest.AI;

using PiratesQuest.Data;
using System;
using System.Collections.Generic;

/// <summary>
/// Single source of truth for every AI ship type.
///
/// To add a new AI ship:
/// 1. add one entry here
/// 2. add a matching `godot/python_ai/<id>/brain.py`
///
/// Nothing else in the C# codebase should need a hardcoded AI type id.
/// </summary>
public static class AiShips
{
  private static readonly AiShipDefinition[] Definitions =
  [
    new()
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
      ShipAvoidanceRange = 90.0f,
      PreferredCombatRange = 48.0f,
      FireRange = 72.0f,
      GoalArrivalDistance = 20.0f,
      PatrolRadius = 250.0f,
      DefaultSpawnCount = 2,
      GoalMode = AiShipGoalMode.NearestHostileShip,
      RewardMode = AiShipRewardMode.None,
      CargoManifest = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Coin, 24 },
        { InventoryItemType.Wood, 18 },
        { InventoryItemType.CannonBall, 8 },
      }
    },
    new()
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
      ShipAvoidanceRange = 150.0f,
      PreferredCombatRange = 0.0f,
      FireRange = 0.0f,
      GoalArrivalDistance = 32.0f,
      PatrolRadius = 0.0f,
      DefaultSpawnCount = 2,
      GoalMode = AiShipGoalMode.NearestPort,
      RewardMode = AiShipRewardMode.None,
      CargoManifest = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Coin, 30 },
        { InventoryItemType.Wood, 12 },
        { InventoryItemType.Iron, 10 },
        { InventoryItemType.Fish, 16 },
      }
    },
    new()
    {
      Id = "neural_patrol",
      DisplayName = "Neural Patrol",
      AllyTypeId = "trader",
      VisualType = AiShipVisualType.PlayerWhite,
      MaxHealth = 120.0f,
      MaxSpeed = 12.0f,
      Acceleration = 4.0f,
      Deceleration = 4.2f,
      TurnSpeed = 0.5f,
      AttackDamage = 0.0f,
      ProjectileBonusSpeed = 0.0f,
      FireCooldownSeconds = 999.0f,
      ShipAvoidanceRange = 125.0f,
      PreferredCombatRange = 0.0f,
      FireRange = 0.0f,
      GoalArrivalDistance = 18.0f,
      PatrolRadius = 250.0f,
      DefaultSpawnCount = 1,
      UsePythonCountOverride = true,
      GoalMode = AiShipGoalMode.RandomPortEpisode,
      RewardMode = AiShipRewardMode.TouchPort,
      CargoManifest = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Coin, 12 },
      }
    }
  ];

  private static readonly Dictionary<string, AiShipDefinition> ById = BuildById();

  public static IReadOnlyList<AiShipDefinition> All => Definitions;
  public static AiShipDefinition Default => Definitions[0];

  public static IReadOnlyList<string> AllIds
  {
    get
    {
      var ids = new string[Definitions.Length];
      for (int i = 0; i < Definitions.Length; i++)
        ids[i] = Definitions[i].Id;
      return ids;
    }
  }

  public static bool IsKnownId(string id)
  {
    return ById.ContainsKey(NormalizeId(id));
  }

  public static AiShipDefinition FromId(string id)
  {
    if (ById.TryGetValue(NormalizeId(id), out var definition))
      return definition;

    return Default;
  }

  public static Dictionary<string, int> BuildSpawnTargetCounts(int pythonAiCountOverride)
  {
    var counts = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (AiShipDefinition definition in Definitions)
    {
      int count = definition.UsePythonCountOverride
        ? Math.Max(0, pythonAiCountOverride)
        : Math.Max(0, definition.DefaultSpawnCount);

      counts[definition.Id] = count;
    }

    return counts;
  }

  private static Dictionary<string, AiShipDefinition> BuildById()
  {
    var map = new Dictionary<string, AiShipDefinition>(StringComparer.Ordinal);
    foreach (AiShipDefinition definition in Definitions)
      map[definition.Id] = definition;
    return map;
  }

  private static string NormalizeId(string id)
  {
    return (id ?? string.Empty).Trim().ToLowerInvariant();
  }
}
