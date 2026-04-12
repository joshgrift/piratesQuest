namespace PiratesQuest.Data;

using System;
using System.Collections.Generic;
using System.Linq;

public enum FeatureUnlock
{
  SellGoods,
  TavernTalk,
  BuyGoods,
  ShipyardComponents,
  ShipTierUpgrades,
  Vault,
}

public enum QuestMetricKind
{
  ShipMovementInputs,
  PortsVisitedCount,
  CameraDrags,
  CannonballsShot,
  ItemsCollected,
  ItemsBought,
  ItemsSold,
  SoldProfit,
  TotalMoneyEarned,
  EquippedComponentCount,
  ShipsSunk,
}

public class QuestStepDefinition
{
  public string Label { get; init; } = "";
  public string PreStepPopupText { get; init; }
  public string PostStepPopupText { get; init; }
  public QuestMetricKind Metric { get; init; }
  public string ItemType { get; init; } = "";
  public int RequiredValue { get; init; }
}

public class QuestDefinition
{
  public string Id { get; init; } = "";
  public string Title { get; init; } = "";
  public string GiverNpcId { get; init; } = "";
  public string GiverName { get; init; } = "";
  public string GiverPortName { get; init; } = "";
  public string OfferText { get; init; } = "";
  public string AcceptedText { get; init; } = "";
  public string Description { get; init; } = "";
  public string CompletionText { get; init; } = "";
  public string[] PrerequisiteQuestIds { get; init; } = [];
  public bool RevealGiverInQuestLog { get; init; } = true;
  public bool CanAcceptFromQuestLog { get; init; }
  public bool AutoAcceptWhenAvailable { get; init; }
  public FeatureUnlock[] Unlocks { get; init; } = [];
  public QuestStepDefinition[] Steps { get; init; } = [];
}

public record QuestStepProgressDto(
  string Label,
  string PreStepPopupText,
  string PostStepPopupText,
  int CurrentValue,
  int RequiredValue,
  bool IsComplete
);

public record QuestSummaryDto(
  string Id,
  string Title,
  string GiverNpcId,
  string GiverName,
  string GiverPortrait,
  string GiverPortName,
  bool RevealGiverInQuestLog,
  bool CanAcceptFromQuestLog,
  string OfferText,
  string AcceptedText,
  string Description,
  string CompletionText,
  string[] Unlocks,
  QuestStepProgressDto[] Steps
);

public record QuestHudStateDto
{
  public QuestSummaryDto[] Available { get; init; } = [];
  public QuestSummaryDto Active { get; init; }
  public QuestSummaryDto[] All { get; init; } = [];
  public string[] CompletedIds { get; init; } = [];
  public string[] RecentlyCompletedIds { get; init; } = [];
  public string[] UnlockedFeatures { get; init; } = [];
}

public static class QuestData
{
  public static readonly string[] CoreTradeGoods = [
    InventoryItemType.Wood.ToString(),
    InventoryItemType.Iron.ToString(),
    InventoryItemType.Fish.ToString(),
    InventoryItemType.Tea.ToString(),
  ];

  public static readonly QuestDefinition[] Quests =
  [
    new QuestDefinition
    {
      Id = "scarlett_learn_to_sail",
      Title = "Learn to Sail",
      GiverNpcId = "scarlett",
      GiverName = "Scarlett",
      CanAcceptFromQuestLog = true,
      AutoAcceptWhenAvailable = true,
      OfferText = null,
      AcceptedText = "Welcome to the Seas! Press W, A, S, or D and make the ship answer. We start with the basics before the sea starts laughing at you.",
      Description = "Scarlett wants to see if you can actually control the ship. Move once with your sailing controls so she knows the helm is in working hands.",
      CompletionText = "Good. The ship listens to you, which puts you ahead of some captains already.",
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Move the ship once",
          Metric = QuestMetricKind.ShipMovementInputs,
          RequiredValue = 1,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "scarlett_drag_camera",
      Title = "Swing the Camera",
      GiverNpcId = "scarlett",
      GiverName = "Scarlett",
      PrerequisiteQuestIds = ["scarlett_learn_to_sail"],
      CanAcceptFromQuestLog = true,
      AutoAcceptWhenAvailable = true,
      OfferText = null,
      AcceptedText = "Click and drag the camera once. A captain who never looks around is just volunteering to get jumped.",
      Description = "Scarlett wants you to stop staring straight ahead like a fresh deckhand. Click and drag to move your camera around once so you can actually watch the sea around you.",
      CompletionText = "There ye go. A captain who can look around is much harder to surprise.",
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Click and drag the camera",
          Metric = QuestMetricKind.CameraDrags,
          RequiredValue = 1,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "scarlett_fire_cannons",
      Title = "Loose a Broadside",
      GiverNpcId = "scarlett",
      GiverName = "Scarlett",
      PrerequisiteQuestIds = ["scarlett_drag_camera"],
      CanAcceptFromQuestLog = true,
      AutoAcceptWhenAvailable = true,
      OfferText = null,
      AcceptedText = "Take the ship out with cannonballs aboard and fire one broadside. Q for port, E for starboard.",
      Description = "Scarlett wants you to fire your cannons once so you get used to broadside combat before the real work starts.",
      CompletionText = "Aye, that's the sound. You won't flinch the first time a fight starts now.",
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Fire your cannons once",
          Metric = QuestMetricKind.CannonballsShot,
          RequiredValue = 1,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "scarlett_sail_to_port",
      Title = "Sail to Port",
      GiverNpcId = "scarlett",
      GiverName = "Scarlett",
      PrerequisiteQuestIds = ["scarlett_fire_cannons"],
      CanAcceptFromQuestLog = true,
      AutoAcceptWhenAvailable = true,
      OfferText = null,
      AcceptedText = "A captain who can't arrive properly is just a floating apology. Find a harbor ring, glide in clean, and let the port panel open.",
      Description = "Scarlett wants proof that you can actually pull into a harbor without turning it into a scene. Sail to any port and dock once. Just move into the harbor interaction ring until the port panel opens.",
      CompletionText = "Nice. You're not just a menace to the sea, you can actually visit places.",
      Unlocks = [FeatureUnlock.SellGoods, FeatureUnlock.TavernTalk, FeatureUnlock.BuyGoods],
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Dock at any port",
          Metric = QuestMetricKind.PortsVisitedCount,
          RequiredValue = 1,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "scarlett_trade_for_merchant",
      Title = "Scarlett's Trade Lesson",
      GiverNpcId = "scarlett",
      GiverName = "Scarlett",
      PrerequisiteQuestIds = ["scarlett_sail_to_port"],
      CanAcceptFromQuestLog = true,
      AutoAcceptWhenAvailable = true,
      OfferText = null,
      AcceptedText = "Gathering keeps you afloat, but trading makes you dangerous. Buy each core good somewhere cheap, sell each one for profit somewhere better, and earn 100 gold total while you're at it.",
      Description = "Scarlett wants you trading on purpose instead of by accident. Buy each core trade good at a port, then sell each one somewhere else for a profit. Losses don't count, so yes, the prices actually matter. Finish by earning 100 gold total.",
      CompletionText = "Better. Now you're trading with your head instead of your feelings. Ship components are unlocked.",
      Unlocks = [FeatureUnlock.ShipyardComponents],
      Steps =
      [
        ..CoreTradeGoods.Select(itemType => new QuestStepDefinition
        {
          Label = $"Buy {itemType}",
          Metric = QuestMetricKind.ItemsBought,
          ItemType = itemType,
          RequiredValue = 1,
        }),
        ..CoreTradeGoods.Select(itemType => new QuestStepDefinition
        {
          Label = $"Sell {itemType} for profit",
          Metric = QuestMetricKind.SoldProfit,
          ItemType = itemType,
          RequiredValue = 1,
        }),
        new QuestStepDefinition
        {
          Label = "Earn 100 gold",
          PreStepPopupText = "Keep trading until you've earned 100 gold total.",
          Metric = QuestMetricKind.TotalMoneyEarned,
          RequiredValue = 100,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "beef_up_your_ship",
      Title = "Beef Up Your Ship",
      GiverNpcId = "elder-bertram",
      GiverName = "Elder Bertram",
      GiverPortName = "Saint Johns",
      PrerequisiteQuestIds = ["scarlett_trade_for_merchant"],
      OfferText = "Your ship still looks half-finished. Buy a proper component and fit it before you come grinning at me.",
      AcceptedText = "Your ship still looks half-finished. Go to a port, buy a proper component, and fit it before you come grinning at me.",
      Description = "Elder Bertram is tired of captains calling a bare deck a build. Visit the shipyard, buy extra components, and equip at least 2 of them at the same time. This checks what you're actively using, not what you're hoarding.",
      CompletionText = "Much better. Your ship finally looks like someone made decisions on purpose. Ship class upgrades are unlocked.",
      Unlocks = [FeatureUnlock.ShipTierUpgrades],
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Equip a component",
          Metric = QuestMetricKind.EquippedComponentCount,
          RequiredValue = 1,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "kill_five_ships",
      Title = "Kill 5 Ships",
      GiverNpcId = "dorian-blackwake",
      GiverName = "Dorian Blackwake",
      GiverPortName = "Krakenfall",
      PrerequisiteQuestIds = ["beef_up_your_ship"],
      OfferText = "You want access to a vault? Prove you can survive a real fight first. Sink five ships and then we can talk.",
      AcceptedText = "Get out there, line up your broadsides, and sink five ships.",
      Description = "Dorian doesn't care about your potential. He cares whether you can finish a fight. Sink 5 ships with your cannons. Q fires your port side and E fires your starboard side, so line up your broadsides and don't let enemy ships limp away.",
      CompletionText = "That got people's attention. You now have access to a vault, which is great, because eventually someone will try to return the favor.",
      Unlocks = [FeatureUnlock.Vault],
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Sink 5 ships",
          Metric = QuestMetricKind.ShipsSunk,
          RequiredValue = 5,
        },
      ],
    },
  ];

  private static readonly Dictionary<string, QuestDefinition> _questsById = Quests
    .ToDictionary(q => q.Id, q => q, StringComparer.Ordinal);

  public static QuestDefinition GetQuest(string questId)
  {
    if (string.IsNullOrWhiteSpace(questId))
      return null;

    _questsById.TryGetValue(questId, out var quest);
    return quest;
  }

  public static string GetQuestGiverPortrait(string npcId)
  {
    if (string.Equals(npcId, "scarlett", StringComparison.Ordinal))
      return "character2.png";

    return TavernData.GetCharacterById(npcId)?.Portrait ?? "";
  }

  public static QuestDefinition[] GetAvailableQuests(IEnumerable<string> completedQuestIds, string currentQuestId)
  {
    var completed = new HashSet<string>(completedQuestIds ?? [], StringComparer.Ordinal);

    return Quests
      .Where(q => !completed.Contains(q.Id))
      .Where(q => !string.Equals(q.Id, currentQuestId, StringComparison.Ordinal))
      .Where(q => q.PrerequisiteQuestIds.All(completed.Contains))
      .ToArray();
  }

  public static string GetQuestNameForFeature(FeatureUnlock feature)
  {
    return Quests.FirstOrDefault(q => q.Unlocks.Contains(feature))?.Title ?? "Unknown Quest";
  }

  public static int GetQuestIndex(string questId)
  {
    if (string.IsNullOrWhiteSpace(questId))
      return -1;

    for (int i = 0; i < Quests.Length; i++)
    {
      if (string.Equals(Quests[i].Id, questId, StringComparison.Ordinal))
        return i;
    }

    return -1;
  }
}
