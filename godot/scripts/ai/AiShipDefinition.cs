namespace PiratesQuest.AI;

using PiratesQuest.Data;
using System.Collections.Generic;

public enum AiShipGoalMode
{
  NearestHostileShip,
  NearestPort,
  RandomPortEpisode
}

public enum AiShipRewardMode
{
  None,
  TouchPort
}

/// <summary>
/// Data-only definition for one AI ship archetype.
/// 
/// Players use components and progression to reach their stats.
/// AI ships are simpler: we just declare the stats directly here.
/// </summary>
public sealed class AiShipDefinition
{
  public string Id { get; init; } = string.Empty;
  public string DisplayName { get; init; } = string.Empty;
  public string AllyTypeId { get; init; } = string.Empty;
  public AiShipVisualType VisualType { get; init; } = AiShipVisualType.RaiderBlack;
  public float MaxHealth { get; init; } = 120.0f;
  public float MaxSpeed { get; init; } = 11.5f;
  public float Acceleration { get; init; } = 3.6f;
  public float Deceleration { get; init; } = 3.8f;
  public float TurnSpeed { get; init; } = 0.45f;
  public float AttackDamage { get; init; } = 20.0f;
  public float ProjectileBonusSpeed { get; init; } = 28.0f;
  public float FireCooldownSeconds { get; init; } = 2.5f;
  public float ShipAvoidanceRange { get; init; } = 110.0f;
  public float PreferredCombatRange { get; init; } = 50.0f;
  public float FireRange { get; init; } = 70.0f;
  public float GoalArrivalDistance { get; init; } = 20.0f;
  public float PatrolRadius { get; init; } = 250.0f;
  public int DefaultSpawnCount { get; init; }
  public bool UsePythonCountOverride { get; init; }
  public AiShipGoalMode GoalMode { get; init; } = AiShipGoalMode.NearestPort;
  public AiShipRewardMode RewardMode { get; init; } = AiShipRewardMode.None;
  public Dictionary<InventoryItemType, int> CargoManifest { get; init; } = [];
}
