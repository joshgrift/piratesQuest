namespace PiratesQuest.Data;

using Godot.Collections;

/// <summary>
/// Defines the cost and component slot count for each ship tier.
/// Tier 1 is the starting ship — no cost required.
/// </summary>
public class ShipTierData
{
  public string Name;
  public string Description;
  public int ComponentSlots;
  public Dictionary<InventoryItemType, int> Cost;
}

public static class GameData
{
  // Ship tiers: each upgrade adds 2 more component slots and unlocks a bigger ship model.
  // Costs are intentionally high — upgrades are meant to be major milestones.
  public static readonly ShipTierData[] ShipTiers = [
    new ShipTierData
    {
      Name = "Sloop",
      Description = "A nimble starter vessel",
      ComponentSlots = 4,
      Cost = new Dictionary<InventoryItemType, int>()
    },
    new ShipTierData
    {
      Name = "Brigantine",
      Description = "A sturdy mid-size warship with extra component slots",
      ComponentSlots = 6,
      Cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Wood, 300 },
        { InventoryItemType.Iron, 250 },
        { InventoryItemType.Fish, 150 },
        { InventoryItemType.Tea, 100 },
        { InventoryItemType.Coin, 2000 }
      }
    },
    new ShipTierData
    {
      Name = "Galleon",
      Description = "A fearsome capital ship with maximum component capacity",
      ComponentSlots = 8,
      Cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Wood, 400 },
        { InventoryItemType.Iron, 300 },
        { InventoryItemType.Fish, 150 },
        { InventoryItemType.Tea, 100 },
        { InventoryItemType.Coin, 5000 }
      }
    }
  ];

  static public Component[] Components = [
    new Component
    {
      name = "Advanced Sails",
      description = "Increases ship acceleration by 25%",
      icon = "acceleration.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Wood, 250 },
        { InventoryItemType.Tea, 50 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipAcceleration,
          Modifier = RecordPlayerStatChangeModifier.Multiplicative,
          Value = 1.25f
        }
      ]
    },
    new Component
    {
      name = "Reinforced Sails",
      description = "Increases ship max speed by 15%",
      icon = "speed.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Wood, 250 },
        { InventoryItemType.Iron, 40 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipMaxSpeed,
          Modifier = RecordPlayerStatChangeModifier.Multiplicative,
          Value = 1.15f
        }
      ]
    },
    new Component
    {
      name = "Reinforced Hull",
      description = "Increases ship health by 50 points",
      icon = "hull.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Iron, 100 },
        { InventoryItemType.Wood, 100 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipHullStrength,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 50.0f
        }
      ]
    },
    new Component
    {
      name = "Expanded Cargo Hold",
      description = "Increases ship capacity by 100",
      icon = "cargo.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Wood, 250 },
        { InventoryItemType.Iron, 50 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipCapacity,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 100.0f
        }
      ]
    },
    new Component
    {
      name = "Masterwork Cannons",
      description = "Increases attack damage by 25%",
      icon = "damage.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Iron, 150 },
        { InventoryItemType.CannonBall, 50 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.AttackDamage,
          Modifier = RecordPlayerStatChangeModifier.Multiplicative,
          Value = 1.25f
        }
      ]
    },
    new Component
    {
      name = "Long-Range Cannons",
      description = "Increases attack range by 20%",
      icon = "range.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Iron, 150 },
        { InventoryItemType.CannonBall, 50 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.AttackRange,
          Modifier = RecordPlayerStatChangeModifier.Multiplicative,
          Value = 1.20f
        }
      ]
    },
    new Component
    {
      name = "Automated Health Regeneration",
      description = "Increases health regeneration by 5 points per minute",
      icon = "heal.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Tea, 250 },
        { InventoryItemType.Fish, 100 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.HealthRegen,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 5.0f
        }
      ]
    },
    new Component
    {
      name = "Advanced Fish Nets",
      description = "Increases fish collection rate by 50%",
      icon = "collect_fish.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Wood, 100 },
        { InventoryItemType.Fish, 50 },
        { InventoryItemType.Tea, 50 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.CollectionFish,
          Modifier = RecordPlayerStatChangeModifier.Multiplicative,
          Value = 1.50f
        }
      ]
    },
    new Component
    {
      name = "Reinforced Lumber Tools",
      description = "Increases wood collection rate by 50%",
      icon = "collect_wood.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Iron, 100 },
        { InventoryItemType.Wood, 50 },
        { InventoryItemType.Tea, 50 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.CollectionWood,
          Modifier = RecordPlayerStatChangeModifier.Multiplicative,
          Value = 1.50f
        }
      ]
    },
    new Component
    {
      name = "Enhanced Mining Tools",
      description = "Increases iron collection rate by 50%",
      icon = "collect_iron.png",
      cost = new Dictionary<InventoryItemType, int>
      {
        { InventoryItemType.Iron, 100 },
        { InventoryItemType.Wood, 100 },
        { InventoryItemType.Tea, 50 }
      },
      statChanges = [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.CollectionIron,
          Modifier = RecordPlayerStatChangeModifier.Multiplicative,
          Value = 1.50f
        }
      ]
    }
  ];
}
