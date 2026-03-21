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
  PortsVisitedCount,
  ItemsCollected,
  ItemsBought,
  ItemsSold,
  SoldProfit,
  TotalMoneyEarned,
  EquippedComponentCount,
  ShipsSunk,
}

public enum QuestTurnInMode
{
  AutoCompleteWhenObjectivesMet,
  CompleteWhenEnteringGiverPort,
}

public class QuestStepDefinition
{
  public string Label { get; init; } = "";
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
  public string Description { get; init; } = "";
  public string CompletionText { get; init; } = "";
  public QuestTurnInMode TurnInMode { get; init; } = QuestTurnInMode.CompleteWhenEnteringGiverPort;
  public string[] PrerequisiteQuestIds { get; init; } = [];
  public bool RevealGiverInQuestLog { get; init; } = true;
  public bool CanAcceptFromQuestLog { get; init; }
  public FeatureUnlock[] Unlocks { get; init; } = [];
  public QuestStepDefinition[] Steps { get; init; } = [];
}

public record QuestStepProgressDto(
  string Label,
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
  string Description,
  string CompletionText,
  bool IsReadyToTurnIn,
  string[] Unlocks,
  QuestStepProgressDto[] Steps
);

public record QuestHudStateDto
{
  public QuestSummaryDto[] Available { get; init; } = [];
  public QuestSummaryDto Active { get; init; }
  public QuestSummaryDto[] All { get; init; } = [];
  public string[] CompletedIds { get; init; } = [];
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
      Id = "scarlett_sail_to_port",
      Title = "Sail to Port",
      GiverNpcId = "scarlett",
      GiverName = "Scarlett",
      TurnInMode = QuestTurnInMode.AutoCompleteWhenObjectivesMet,
      CanAcceptFromQuestLog = true,
      Description = "Scarlett wants proof that you can actually pull into a harbor without turning it into a scene. Sail to any port and dock once. Just move into the harbor interaction ring until the port panel opens.",
      CompletionText = "Nice. You made it into port cleanly. You can talk to me in the crew panel one the ride side of your screen anytime. Check the Quests panel for your next quest!",
      Unlocks = [FeatureUnlock.SellGoods, FeatureUnlock.TavernTalk],
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
      Id = "harvest_for_someone",
      Title = "Harvest For Someone",
      GiverNpcId = "governor-caspian",
      GiverName = "Governor Caspian Vale",
      GiverPortName = "Haven",
      PrerequisiteQuestIds = ["scarlett_sail_to_port"],
      Description = "Governor Caspian wants to know whether you're useful or just enthusiastic. Find the red harvest circles in the world, get close enough for the collection marker to appear, and bring back 1 Wood, 1 Iron, 1 Fish, and 1 Tea.",
      CompletionText = "That'll do. You proved you can supply yourself without immediately becoming someone else's problem. Buying goods is now unlocked.",
      Unlocks = [FeatureUnlock.BuyGoods],
      Steps = CoreTradeGoods.Select(itemType => new QuestStepDefinition
      {
        Label = $"Collect 1 {itemType}",
        Metric = QuestMetricKind.ItemsCollected,
        ItemType = itemType,
        RequiredValue = 1,
      }).ToArray(),
    },
    new QuestDefinition
    {
      Id = "trade_for_merchant",
      Title = "Trade for the Merchant",
      GiverNpcId = "gideon-gearlock",
      GiverName = "Gideon Gearlock",
      GiverPortName = "Saint Johns",
      PrerequisiteQuestIds = ["harvest_for_someone"],
      Description = "Gideon wants you to stop guessing and start trading on purpose. Buy each core trade good at a port, then sell each one somewhere else for a profit. Losses don't count, so yes, the prices actually matter. Finish by earning 100 gold total.",
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
      PrerequisiteQuestIds = ["scarlett_sail_to_port"],
      Description = "Elder Bertram is tired of captains calling a bare deck a build. Visit the shipyard, buy extra components, and equip at least 2 of them at the same time. This checks what you're actively using, not what you're hoarding.",
      CompletionText = "Much better. Your ship finally looks like someone made decisions on purpose. Ship class upgrades are unlocked.",
      Unlocks = [FeatureUnlock.ShipTierUpgrades],
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Equip 2 components",
          Metric = QuestMetricKind.EquippedComponentCount,
          RequiredValue = 2,
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
      PrerequisiteQuestIds = ["scarlett_sail_to_port"],
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
