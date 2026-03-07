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
      Id = "scarred-gunner",
      Name = "Briggs",
      Role = "Deck Gunner",
      PortName = "Saint Johns",
      Portrait = "character6.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.AttackDamage,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 6.0f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "quartermaster-mira",
      Name = "Mira",
      Role = "Quartermaster",
      PortName = "Krakenfall",
      Portrait = "character13.png",
      Hireable = true,
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipCapacity,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 120.0f,
        },
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipTurnSpeed,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 0.05f,
        },
      ],
    },
    new TavernCharacterData
    {
      Id = "dockside-poet",
      Name = "Pip",
      Role = "Dockside Poet",
      PortName = "Haven",
      Portrait = "character20.png",
      Hireable = false,
      StatChanges = [],
    },
    new TavernCharacterData
    {
      Id = "lazy-lookout",
      Name = "Old Ned",
      Role = "Lookout",
      PortName = "Haven",
      Portrait = "character24.png",
      Hireable = true,
      StatChanges = [],
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
