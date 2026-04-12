namespace PiratesQuest.Data;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One shop item entry owned by a port definition.
/// This stays as plain C# data so the authored values are easy to read and edit.
/// </summary>
public class PortShopItemDefinition
{
  public InventoryItemType ItemType { get; init; } = InventoryItemType.Wood;
  public int BuyPrice { get; init; }
  public int SellPrice { get; init; }
}

/// <summary>
/// One tavern NPC definition. NPCs are authored separately from ports.
/// </summary>
public class PortNpcDefinition
{
  public string Id { get; init; } = "";
  public string Name { get; init; } = "";
  public string Role { get; init; } = "";
  public string Portrait { get; init; } = "";
  public bool Hireable { get; init; }
  public string[] TalkPhrases { get; init; } = [];
  public string HireText { get; init; } = "";
  public string FireText { get; init; } = "";
  public RecordPlayerStatChange[] StatChanges { get; init; } = [];
}

/// <summary>
/// Full authored definition for a single port.
/// The scene only owns placement, interaction area, and PortId.
/// </summary>
public class PortDefinition
{
  public string Id { get; init; } = "";
  public string DisplayName { get; init; } = "";
  public PortShopItemDefinition[] ItemsForSale { get; init; } = [];
  public string[] CharacterIds { get; init; } = [];
}

/// <summary>
/// Central lookup helper for all port-owned gameplay data.
/// This is the single source of truth for port ids, display names,
/// shop inventory, and tavern NPC definitions.
/// </summary>
public static class PortData
{
  public const string ScarlettId = "scarlett";

  private static readonly Lazy<Dictionary<string, PortDefinition>> _portsById = new(BuildPortsById);
  private static readonly Lazy<Dictionary<string, string>> _portIdsByName = new(BuildPortIdsByName);
  private static readonly Lazy<Dictionary<string, PortNpcDefinition>> _npcsById = new(BuildNpcsById);
  public static IReadOnlyCollection<string> PortIds => _portsById.Value.Keys;

  private static readonly PortDefinition[] _ports =
  [
    new()
    {
      Id = "saint-johns",
      DisplayName = "Saint Johns",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 5, SellPrice = 4 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 10, SellPrice = 5 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 0 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 8 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 50, SellPrice = 40 },
        new() { ItemType = InventoryItemType.Trophy, BuyPrice = 100000, SellPrice = 0 },
      ],
      CharacterIds = ["gideon-gearlock", "tommy-fuse", "elder-bertram"],
    },
    new()
    {
      Id = "rusthook-point",
      DisplayName = "Rusthook Point",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 5, SellPrice = 4 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 10, SellPrice = 5 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 0 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 8 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 50, SellPrice = 40 },
        new() { ItemType = InventoryItemType.Trophy, BuyPrice = 100000, SellPrice = 0 },
      ],
      CharacterIds = [],
    },
    new()
    {
      Id = "shard-bay",
      DisplayName = "Shard Bay",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 5, SellPrice = 4 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 10, SellPrice = 5 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 0 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 8 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 50, SellPrice = 40 },
        new() { ItemType = InventoryItemType.Trophy, BuyPrice = 100000, SellPrice = 0 },
      ],
      CharacterIds = [],
    },
    new()
    {
      Id = "krakenfall",
      DisplayName = "Krakenfall",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 4, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 1 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 8, SellPrice = 5 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 50, SellPrice = 45 },
        new() { ItemType = InventoryItemType.Trophy, BuyPrice = 100000, SellPrice = 0 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 8 },
      ],
      CharacterIds = ["dorian-blackwake", "valora-rumwhisper", "harlan-bentbeam"],
    },
    new()
    {
      Id = "tidefall-island",
      DisplayName = "Tidefall Island",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 4, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 1 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 8, SellPrice = 5 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 50, SellPrice = 45 },
        new() { ItemType = InventoryItemType.Trophy, BuyPrice = 100000, SellPrice = 0 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 8 },
      ],
      CharacterIds = [],
    },
    new()
    {
      Id = "haven-harbour",
      DisplayName = "Haven Harbour",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 4, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 1 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 8, SellPrice = 5 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 50, SellPrice = 45 },
        new() { ItemType = InventoryItemType.Trophy, BuyPrice = 100000, SellPrice = 0 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 0 },
      ],
      CharacterIds = ["governor-caspian", "merrick-ash", "rafael-tide", "silas-quill"],
    },
    new()
    {
      Id = "pebblehook-bay",
      DisplayName = "Pebblehook Bay",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 4, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 1 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 8, SellPrice = 5 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 50, SellPrice = 45 },
        new() { ItemType = InventoryItemType.Trophy, BuyPrice = 100000, SellPrice = 0 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 0 },
      ],
      CharacterIds = [],
    },
    new()
    {
      Id = "spire-harbour",
      DisplayName = "Spire Harbour",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 4, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 1 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 8, SellPrice = 5 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 50, SellPrice = 45 },
        new() { ItemType = InventoryItemType.Trophy, BuyPrice = 100000, SellPrice = 0 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 0 },
      ],
      CharacterIds = [],
    },
  ];

  private static readonly PortNpcDefinition[] _characters =
  [
    new()
    {
      Id = ScarlettId,
      Name = "Scarlett",
      Role = "First Mate",
      Portrait = "character2.png",
      Hireable = false,
      TalkPhrases =
      [
        "Keep your eyes moving, captain. The sea punishes tunnel vision faster than bad luck ever could.",
        "If a job feels too safe, you probably missed the dangerous part.",
        "Quests teach the ropes, but good habits keep your ship afloat after the lesson ends.",
        "Ports are for selling, repairing, and making smarter mistakes than the last voyage.",
      ],
    },
    new()
    {
      Id = "gideon-gearlock",
      Name = "Gideon Gearlock",
      Role = "Merchant Broker",
      Portrait = "character8.png",
      Hireable = true,
      TalkPhrases =
      [
        "Trade routes reward patience. Nervous captains bleed coin faster than cannonballs.",
        "Watch which ports run short after storms. The market usually tells you where to sail next.",
        "Profit is mostly discipline wearing a nicer coat.",
      ],
      HireText = "Give me a bunk and I'll tighten up your sales. I prefer captains who can count.",
      FireText = "Then keep your own books, Captain. Try not to drown in bad pricing.",
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
    new()
    {
      Id = "tommy-fuse",
      Name = "Tommy Fuse",
      Role = "Powder Runner",
      Portrait = "character7.png",
      Hireable = true,
      TalkPhrases =
      [
        "Dry powder, steady hands, and no panic shots. That's how you make thunder worth hearing.",
        "Fast crews are drilled crews. Chaos just makes louder mistakes.",
        "Keep cannonballs stocked and I'll keep smiling.",
      ],
      HireText = "Say the word and I'll have your gun deck snapping to rhythm before sunset.",
      FireText = "Fair enough. I'll go find another ship that appreciates a proper broadside.",
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
    new()
    {
      Id = "elder-bertram",
      Name = "Elder Bertram",
      Role = "Retired Shipwright",
      Portrait = "character17.png",
      Hireable = true,
      TalkPhrases =
      [
        "A strong hull is just a pile of careful choices nailed together.",
        "Most captains notice my work right after a hit fails to sink them.",
        "Timber complains less than sailors. That's one reason I still like it.",
      ],
      HireText = "Keep your word and I'll keep your hull alive. That's the whole arrangement.",
      FireText = "Understood. I'll return ashore quietly and let the planks judge you from here.",
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
    new()
    {
      Id = "dorian-blackwake",
      Name = "Dorian Blackwake",
      Role = "Broken Cannoneer",
      Portrait = "character31.png",
      Hireable = true,
      TalkPhrases =
      [
        "Hesitation kills faster than cannon fire. Decide, then commit.",
        "Broadside discipline wins fights. Hero nonsense just makes wreckage.",
        "If a gun deck goes quiet, someone already made a mistake.",
      ],
      HireText = "Good. Give me powder, space, and silence when I'm working.",
      FireText = "Fine. The sea still remembers who taught your gunners.",
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
    new()
    {
      Id = "valora-rumwhisper",
      Name = "Valora Rumwhisper",
      Role = "Rumor Broker",
      Portrait = "character15.png",
      Hireable = false,
      TalkPhrases =
      [
        "Rumors travel faster than ships and lie less often than captains.",
        "Krakenfall watches black powder more closely than most people realize.",
        "Buy a rumor before you buy trouble. It's usually cheaper.",
      ],
    },
    new()
    {
      Id = "harlan-bentbeam",
      Name = "Harlan Bentbeam",
      Role = "Master Woodworker",
      Portrait = "character28.png",
      Hireable = true,
      TalkPhrases =
      [
        "Good repairs start before the hull is desperate.",
        "Small damage turns expensive when nobody respects the joints.",
        "Reliable work beats dramatic work more often than sailors admit.",
      ],
      HireText = "Yes. I'll keep your hull breathing between fights.",
      FireText = "Understood. I'll leave your ship better than I found it.",
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
    new()
    {
      Id = "governor-caspian",
      Name = "Governor Caspian Vale",
      Role = "Acting Governor",
      Portrait = "character18.png",
      Hireable = false,
      TalkPhrases =
      [
        "Privateers broke the peace first. Raiders merely improved the technique.",
        "Dark fleet captains overcommit when reefs steal their escape routes.",
        "Administration is mostly managing disasters before they become traditions.",
      ],
    },
    new()
    {
      Id = "merrick-ash",
      Name = "Merrick Ash",
      Role = "Lumberjack",
      Portrait = "character25.png",
      Hireable = true,
      TalkPhrases =
      [
        "Wood runs go bad when people wander and call it teamwork.",
        "Cut, stack, move. The trees don't care about excuses.",
        "A clean haul beats a loud one.",
      ],
      HireText = "Fine. I work, you steer, and nobody wastes daylight pretending to be busy.",
      FireText = "Aye. I'll be with the lumber piles if you need sense later.",
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
    new()
    {
      Id = "rafael-tide",
      Name = "Rafael Tide",
      Role = "Harpoon Charmer",
      Portrait = "character3.png",
      Hireable = true,
      TalkPhrases =
      [
        "Read the current lines right and the hold fills itself.",
        "Most crews fish where it's easy, then wonder why supper feels insulting.",
        "Charm helps, but timing lands the catch.",
      ],
      HireText = "Invite me aboard and I'll turn wandering fishing runs into proper hauls.",
      FireText = "No hard feelings. The sea and I still get along just fine without you.",
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
    new()
    {
      Id = "silas-quill",
      Name = "Silas Quill",
      Role = "Stone Sculptor",
      Portrait = "character23.png",
      Hireable = true,
      TalkPhrases =
      [
        "Stone rewards rhythm. Swing sloppy and the island laughs at you.",
        "Good mining looks patient right up until the hold starts filling fast.",
        "Iron comes easier when the crew stops fighting the rock.",
      ],
      HireText = "Give me a berth and I'll make your mining runs cleaner and quicker.",
      FireText = "Very well. I'll take my hammer back to the shore.",
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

  public static PortDefinition GetPortById(string portId)
  {
    if (string.IsNullOrWhiteSpace(portId))
      return null;

    _portsById.Value.TryGetValue(portId, out var port);
    return port;
  }

  public static string GetPortDisplayName(string portId)
  {
    return GetPortById(portId)?.DisplayName ?? portId ?? "";
  }

  public static string ResolvePortId(string portIdOrName)
  {
    if (string.IsNullOrWhiteSpace(portIdOrName))
      return null;

    if (_portsById.Value.ContainsKey(portIdOrName))
      return portIdOrName;

    return _portIdsByName.Value.TryGetValue(portIdOrName, out var resolvedId)
      ? resolvedId
      : null;
  }

  public static PortShopItemDefinition[] GetItemsForSale(string portId)
  {
    return GetPortById(portId)?.ItemsForSale ?? [];
  }

  public static PortNpcDefinition[] GetCharactersForPortId(string portId)
  {
    var port = GetPortById(portId);
    if (port == null)
      return [];

    return port.CharacterIds
      .Select(GetCharacterById)
      .Where(character => character != null)
      .ToArray();
  }

  public static PortNpcDefinition GetCharacterById(string id)
  {
    if (string.IsNullOrWhiteSpace(id))
      return null;

    _npcsById.Value.TryGetValue(id, out var npc);
    return npc;
  }

  public static string GetPortIdForCharacter(string characterId)
  {
    if (string.IsNullOrWhiteSpace(characterId))
      return null;

    return _ports
      .FirstOrDefault(port => port.CharacterIds.Contains(characterId, StringComparer.Ordinal))
      ?.Id;
  }

  public static HashSet<string> GetCharacterIdSet()
  {
    return _npcsById.Value.Keys.ToHashSet(StringComparer.Ordinal);
  }

  private static Dictionary<string, PortDefinition> BuildPortsById()
  {
    return _ports.ToDictionary(port => port.Id, port => port, StringComparer.Ordinal);
  }

  private static Dictionary<string, string> BuildPortIdsByName()
  {
    var portIdsByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var port in _ports)
    {
      AddPortNameAlias(portIdsByName, port.Id, port.DisplayName);
    }

    return portIdsByName;
  }

  private static Dictionary<string, PortNpcDefinition> BuildNpcsById()
  {
    return _characters.ToDictionary(npc => npc.Id, npc => npc, StringComparer.Ordinal);
  }

  private static void AddPortNameAlias(Dictionary<string, string> portIdsByName, string portId, string portName)
  {
    if (!string.IsNullOrWhiteSpace(portName))
      portIdsByName[portName] = portId;
  }
}
