namespace PiratesQuest.Data;

using Godot;
using Godot.Collections;

public static class GameData
{
  static public Component[] Components = [
    new Component
    {
      name = "Advanced Sails",
      description = "Increases ship acceleration by 25%",
      icon = GD.Load<Texture2D>("res://art/components/acceleration.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/speed.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/hull.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/cargo.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/damage.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/range.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/heal.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/collect_fish.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/collect_wood.png"),
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
      icon = GD.Load<Texture2D>("res://art/components/collect_iron.png"),
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
