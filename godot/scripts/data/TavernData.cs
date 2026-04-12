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
  public string[] TalkPhrases { get; init; } = [];
  public string HireText { get; init; } = "";
  public string FireText { get; init; } = "";
  public RecordPlayerStatChange[] StatChanges { get; init; } = [];
}

public static class TavernData
{
  public const string ScarlettId = "scarlett";

  public static readonly TavernCharacterData[] Characters =
  [
    new TavernCharacterData
    {
      Id = ScarlettId,
      Name = "Scarlett",
      Role = "First Mate",
      PortName = "",
      Portrait = "character2.png",
      Hireable = false,
      TalkPhrases =
      [
        "Keep your eyes moving, captain. The sea punishes tunnel vision faster than bad luck ever could.",
        "If a job feels too safe, you probably missed the dangerous part.",
        "Quests teach the ropes, but good habits keep your ship afloat after the lesson ends.",
        "Ports are for selling, repairing, and making smarter mistakes than the last voyage.",
      ],
      StatChanges = [],
    },
    new TavernCharacterData
    {
      Id = "gideon-gearlock",
      Name = "Gideon Gearlock",
      Role = "Merchant Broker",
      PortName = "Saint Johns",
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
    new TavernCharacterData
    {
      Id = "tommy-fuse",
      Name = "Tommy Fuse",
      Role = "Powder Runner",
      PortName = "Saint Johns",
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
    new TavernCharacterData
    {
      Id = "elder-bertram",
      Name = "Elder Bertram",
      Role = "Retired Shipwright",
      PortName = "Saint Johns",
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
    new TavernCharacterData
    {
      Id = "dorian-blackwake",
      Name = "Dorian Blackwake",
      Role = "Broken Cannoneer",
      PortName = "Krakenfall",
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
    new TavernCharacterData
    {
      Id = "valora-rumwhisper",
      Name = "Valora Rumwhisper",
      Role = "Rumor Broker",
      PortName = "Krakenfall",
      Portrait = "character15.png",
      Hireable = false,
      TalkPhrases =
      [
        "Rumors travel faster than ships and lie less often than captains.",
        "Krakenfall watches black powder more closely than most people realize.",
        "Buy a rumor before you buy trouble. It's usually cheaper.",
      ],
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
    new TavernCharacterData
    {
      Id = "governor-caspian",
      Name = "Governor Caspian Vale",
      Role = "Acting Governor",
      PortName = "Haven",
      Portrait = "character18.png",
      Hireable = false,
      TalkPhrases =
      [
        "Privateers broke the peace first. Raiders merely improved the technique.",
        "Dark fleet captains overcommit when reefs steal their escape routes.",
        "Administration is mostly managing disasters before they become traditions.",
      ],
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
    new TavernCharacterData
    {
      Id = "rafael-tide",
      Name = "Rafael Tide",
      Role = "Harpoon Charmer",
      PortName = "Haven",
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
    new TavernCharacterData
    {
      Id = "silas-quill",
      Name = "Silas Quill",
      Role = "Stone Sculptor",
      PortName = "Haven",
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
