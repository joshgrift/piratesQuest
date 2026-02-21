namespace PiratesQuest.Data;

public enum PlayerStat
{
  MaxHealth,
  HealthRegen,
  AttackDamage,
  AttackRange,
  ShipAcceleration,
  ShipDeceleration,
  ShipMaxSpeed,
  ShipTurnSpeed,
  ShipCapacity,
  ShipHullStrength,
  CollectionFish,
  CollectionWood,
  CollectionIron,
  ComponentCapacity,
}

public enum RecordPlayerStatChangeModifier
{
  Additive,
  Multiplicative
}

public record RecordPlayerStatChange
{
  public PlayerStat Stat;
  public RecordPlayerStatChangeModifier Modifier;
  public float Value;
}