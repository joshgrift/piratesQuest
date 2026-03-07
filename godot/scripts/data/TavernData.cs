namespace PiratesQuest.Data;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static tavern roster data used by both gameplay logic and webview payloads.
/// Each character appears in exactly one port.
/// </summary>
public class TavernCharacterData
{
  public string Id { get; init; } = "";
  public string Name { get; init; } = "";
  public string Role { get; init; } = "";
  public string PortName { get; init; } = "";
  public string Portrait { get; init; } = "";
  public bool Hireable { get; init; }
  public RecordPlayerStatChange[] StatChanges { get; init; } = [];
}

public static class TavernData
{
  public static readonly TavernCharacterData[] Characters =
  [
    new TavernCharacterData
    {
      Id = "gideon-gearlock",
      Name = "Gideon Gearlock",
      Role = "Merchant Broker",
      PortName = "Saint Johns",
      Portrait = "character8.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.SellPriceBonus,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 0.005f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "tommy-fuse",
      Name = "Tommy Fuse",
      Role = "Powder Runner",
      PortName = "Saint Johns",
      Portrait = "character7.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          // AttackRange currently feeds projectile launch speed in the firing code.
          Stat = PlayerStat.AttackRange,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 3.0f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "elder-bertram",
      Name = "Elder Bertram",
      Role = "Retired Shipwright",
      PortName = "Saint Johns",
      Portrait = "character17.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipHullStrength,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 12.0f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "dorian-blackwake",
      Name = "Dorian Blackwake",
      Role = "Broken Cannoneer",
      PortName = "Krakenfall",
      Portrait = "character31.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.AttackDamage,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 5.0f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "valora-rumwhisper",
      Name = "Valora Rumwhisper",
      Role = "Rumor Broker",
      PortName = "Krakenfall",
      Portrait = "character15.png",
      Hireable = false,
      StatChanges = [],
    },
    new TavernCharacterData
    {
      Id = "harlan-bentbeam",
      Name = "Harlan Bentbeam",
      Role = "Master Woodworker",
      PortName = "Krakenfall",
      Portrait = "character28.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.HealthRegen,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 1.0f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "governor-caspian",
      Name = "Governor Caspian Vale",
      Role = "Acting Governor",
      PortName = "Haven",
      Portrait = "character18.png",
      Hireable = false,
      StatChanges = [],
    },
    new TavernCharacterData
    {
      Id = "merrick-ash",
      Name = "Merrick Ash",
      Role = "Lumberjack",
      PortName = "Haven",
      Portrait = "character25.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.CollectionWood,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 0.15f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "rafael-tide",
      Name = "Rafael Tide",
      Role = "Harpoon Charmer",
      PortName = "Haven",
      Portrait = "character3.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.CollectionFish,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 0.15f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "silas-quill",
      Name = "Silas Quill",
      Role = "Stone Sculptor",
      PortName = "Haven",
      Portrait = "character23.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          // Stone nodes currently use the iron collection stat in gameplay code.
          Stat = PlayerStat.CollectionIron,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 0.15f,
        },
      ],
    },
  ];

  public static TavernCharacterData[] GetCharactersForPort(string portName)
  {
    if (string.IsNullOrWhiteSpace(portName)) return [];

    return Characters
      .Where(c => string.Equals(c.PortName, portName, StringComparison.OrdinalIgnoreCase))
      .ToArray();
  }

  public static TavernCharacterData GetCharacterById(string id)
  {
    if (string.IsNullOrWhiteSpace(id)) return null;

    return Characters.FirstOrDefault(c =>
      string.Equals(c.Id, id, StringComparison.Ordinal));
  }

  public static HashSet<string> GetCharacterIdSet()
  {
    return Characters
      .Select(c => c.Id)
      .ToHashSet(StringComparer.Ordinal);
  }
}
