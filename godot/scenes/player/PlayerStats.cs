namespace PiratesQuest;

using Godot.Collections;
using PiratesQuest.Data;

public class PlayerStats
{
  public Dictionary<PlayerStat, float> Stats = new();

  public PlayerStats()
  {
    ResetStats();
  }

  public void ResetStats()
  {
    // Lower base max speed so sailing feels more deliberate and less twitchy.
    Stats[PlayerStat.ShipMaxSpeed] = 7.0f;
    Stats[PlayerStat.ShipAcceleration] = 3.0f;
    // Lower turn speed so steering feels heavier and more intentional.
    Stats[PlayerStat.ShipTurnSpeed] = 0.35f;
    Stats[PlayerStat.ShipHullStrength] = 100.0f;
    Stats[PlayerStat.ShipDeceleration] = 4.0f;
    Stats[PlayerStat.AttackDamage] = 25.0f;
    Stats[PlayerStat.AttackRange] = 30.0f;
    Stats[PlayerStat.HealthRegen] = 0.0f;
    Stats[PlayerStat.CollectionFish] = 1.0f;
    Stats[PlayerStat.CollectionWood] = 1.0f;
    Stats[PlayerStat.CollectionIron] = 1.0f;
    Stats[PlayerStat.ShipCapacity] = 1000.0f;
    Stats[PlayerStat.ComponentCapacity] = 4.0f;
  }

  public float GetStat(PlayerStat stat)
  {
    if (Stats.TryGetValue(stat, out float value))
    {
      return value;
    }
    return 0.0f;
  }

  public void ApplyStatChange(RecordPlayerStatChange statChange)
  {
    if (!Stats.ContainsKey(statChange.Stat))
    {
      Stats.Add(statChange.Stat, 0.0f);
    }

    switch (statChange.Modifier)
    {
      case RecordPlayerStatChangeModifier.Additive:
        Stats[statChange.Stat] += statChange.Value;
        break;
      case RecordPlayerStatChangeModifier.Multiplicative:
        Stats[statChange.Stat] *= statChange.Value;
        break;
    }
  }

  public Dictionary<PlayerStat, float> GetAllStats()
  {
    return Stats;
  }
}
