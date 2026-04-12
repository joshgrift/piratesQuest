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
  TalkedToNpc,
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
  public bool Repeatable { get; init; }
  public string RewardCrewNpcId { get; init; } = "";
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
  bool CanCancel,
  string OfferText,
  string AcceptedText,
  string Description,
  string CompletionText,
  string RewardCrewNpcId,
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
      CompletionText = "Much better. Your ship finally looks like someone made decisions on purpose. Now go find more quests by talking to other characters in ports!",
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
    CreateHireQuest(
      "hire_gideon_gearlock",
      "gideon-gearlock",
      "Close a sale worth 300 gold",
      QuestMetricKind.TotalMoneyEarned,
      requiredValue: 300,
      offerText: "I can squeeze a better price out of every sale, but I do not board for dreamers. Bring in 300 gold from your trading, then come back and show me you can keep a ledger and a course at the same time.",
      acceptedText: "Make 300 gold through honest selling, then come talk to me again. If you can turn cargo into coin, I will turn your markets into profit.",
      description: "Gideon will join your crew and improve your sale prices, but only after you prove you can actually trade. Earn 300 gold after accepting his offer, then return to Gideon in Saint Johns and talk to him to finish the deal.",
      completionText: "Those numbers will do nicely. I am aboard, Captain, and your sales will start looking sharper immediately."
    ),
    CreateHireQuest(
      "hire_tommy_fuse",
      "tommy-fuse",
      "Fire 5 cannonballs",
      QuestMetricKind.CannonballsShot,
      requiredValue: 5,
      offerText: "I make your broadsides reach farther, but I do not sign on with captains who flinch at the guns. Fire five cannonballs, then come back and prove you can keep a firing line moving.",
      acceptedText: "Loose five cannonballs, then report back to me. Show me you can keep the guns talking and I will give your broadside more bite.",
      description: "Tommy boosts your cannon range, but he wants to see live powder first. Fire 5 cannonballs after taking his quest, then return to Tommy in Saint Johns and talk to him to seal the hire.",
      completionText: "That sounded disciplined enough for me. I am on your gun deck now, and your shots will fly farther for it."
    ),
    CreateHireQuest(
      "hire_elder_bertram",
      "elder-bertram",
      "Equip 2 components",
      QuestMetricKind.EquippedComponentCount,
      requiredValue: 2,
      offerText: "I reinforce hulls for captains who respect preparation. Fit at least two proper components to your ship, then return and prove you build with purpose before I lend you my craft.",
      acceptedText: "Outfit your ship with at least two equipped components, then come speak with me again. A captain who prepares their vessel earns stronger planks.",
      description: "Elder Bertram increases your hull strength, but he only works with captains who invest in their ship. Equip 2 components at the same time, then return to Elder Bertram in Saint Johns and talk to him to finish recruiting him.",
      completionText: "Now that looks like a ship worth reinforcing. I will join you, and your hull will hold up better under pressure."
    ),
    CreateHireQuest(
      "hire_dorian_blackwake",
      "dorian-blackwake",
      "Sink 1 ship",
      QuestMetricKind.ShipsSunk,
      requiredValue: 1,
      offerText: "I harden every broadside I touch, but I do not waste that on soft captains. Sink a ship, then come back and prove you can finish a fight before I join your guns.",
      acceptedText: "Sink one ship, then return to me. If you can end a battle cleanly, I will make the next one hurt even more.",
      description: "Dorian increases your cannon damage, but he wants proof you can win a real fight. Sink 1 ship after accepting this quest, then return to Dorian in Krakenfall and talk to him to recruit him.",
      completionText: "You finished the job. Good. I am aboard now, and your broadsides will hit harder because of it."
    ),
    CreateHireQuest(
      "hire_harlan_bentbeam",
      "harlan-bentbeam",
      "Collect 8 Wood",
      QuestMetricKind.ItemsCollected,
      itemType: nameof(InventoryItemType.Wood),
      requiredValue: 8,
      offerText: "I keep hulls knitting themselves back together, but only for captains who respect the timber. Bring back 8 wood after honest work, then talk to me and prove you know what keeps a ship alive.",
      acceptedText: "Gather 8 wood and then return to me. Show me you can keep good material moving and I will keep your hull mending between fights.",
      description: "Harlan improves your hull regeneration, but he wants to see that you understand the value of solid timber. Collect 8 Wood after accepting this quest, then return to Harlan in Krakenfall and talk to him to recruit him.",
      completionText: "That is usable lumber. I will join your crew, and your hull will start recovering itself more reliably."
    ),
    CreateHireQuest(
      "hire_merrick_ash",
      "merrick-ash",
      "Collect 12 Wood",
      QuestMetricKind.ItemsCollected,
      itemType: nameof(InventoryItemType.Wood),
      requiredValue: 12,
      offerText: "I can speed up your wood hauls, but I do not work for captains who only admire trees from the deck. Bring me proof with 12 wood in the hold, then come talk to me and show me you can finish a proper run.",
      acceptedText: "Load up 12 wood, then return and speak with me. If you can run timber without wasting daylight, I will make every future haul better.",
      description: "Merrick improves wood collection, but he only signs on after seeing a real lumber run. Collect 12 Wood after accepting this quest, then return to Merrick in Haven and talk to him to recruit him.",
      completionText: "That is a respectable haul. I am aboard now, Captain, and your wood runs will move quicker from here."
    ),
    CreateHireQuest(
      "hire_rafael_tide",
      "rafael-tide",
      "Collect 10 Fish",
      QuestMetricKind.ItemsCollected,
      itemType: nameof(InventoryItemType.Fish),
      requiredValue: 10,
      offerText: "I make fishing runs pay off, but only for captains who can read the water instead of begging it. Bring in 10 fish, then return and prove your timing is worth backing.",
      acceptedText: "Catch 10 fish, then come talk to me again. Show me you can fill a hold from the sea and I will make every future catch better.",
      description: "Rafael improves fish collection, but he wants proof that you can actually work the water. Collect 10 Fish after accepting this quest, then return to Rafael in Haven and talk to him to recruit him.",
      completionText: "You read the water well enough for me. I am aboard now, and your fishing runs will come in stronger."
    ),
    CreateHireQuest(
      "hire_silas_quill",
      "silas-quill",
      "Collect 10 Iron",
      QuestMetricKind.ItemsCollected,
      itemType: nameof(InventoryItemType.Iron),
      requiredValue: 10,
      offerText: "I can make your mining runs cleaner, but I do not sign on with anyone who cannot work stone with patience. Bring back 10 iron, then return and prove you can keep a haul steady.",
      acceptedText: "Mine 10 iron, then come speak with me. If you can pull useful stone without making a mess of it, I will sharpen every future run.",
      description: "Silas improves iron collection, but he only joins captains who can bring ore home the hard way. Collect 10 Iron after accepting this quest, then return to Silas in Haven and talk to him to recruit him.",
      completionText: "That is solid work. I will join your crew, and your mining trips will start paying out better."
    ),
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

  public static QuestDefinition[] GetAvailableQuests(IEnumerable<string> completedQuestIds, string currentQuestId, IEnumerable<string> hiredCrewIds = null)
  {
    var completed = new HashSet<string>(completedQuestIds ?? [], StringComparer.Ordinal);
    var hiredCrew = new HashSet<string>(hiredCrewIds ?? [], StringComparer.Ordinal);

    return Quests
      .Where(q => q.Repeatable || !completed.Contains(q.Id))
      .Where(q => !string.Equals(q.Id, currentQuestId, StringComparison.Ordinal))
      .Where(q => q.PrerequisiteQuestIds.All(completed.Contains))
      .Where(q => string.IsNullOrWhiteSpace(q.RewardCrewNpcId) || !hiredCrew.Contains(q.RewardCrewNpcId))
      .ToArray();
  }

  public static QuestDefinition GetHireQuestForCharacter(string characterId)
  {
    if (string.IsNullOrWhiteSpace(characterId))
      return null;

    return Quests.FirstOrDefault(q =>
      string.Equals(q.RewardCrewNpcId, characterId, StringComparison.Ordinal));
  }

  private static QuestDefinition CreateHireQuest(
    string id,
    string characterId,
    string firstStepLabel,
    QuestMetricKind firstStepMetric,
    int requiredValue,
    string offerText,
    string acceptedText,
    string description,
    string completionText,
    string itemType = "")
  {
    var character = TavernData.GetCharacterById(characterId);
    if (character == null)
      throw new InvalidOperationException($"Cannot create hire quest for missing character '{characterId}'.");

    return new QuestDefinition
    {
      Id = id,
      Title = $"Earn {character.Name}'s Trust",
      GiverNpcId = character.Id,
      GiverName = character.Name,
      GiverPortName = character.PortName,
      OfferText = offerText,
      AcceptedText = acceptedText,
      Description = description,
      CompletionText = completionText,
      PrerequisiteQuestIds = ["scarlett_sail_to_port"],
      Repeatable = true,
      RewardCrewNpcId = character.Id,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = firstStepLabel,
          Metric = firstStepMetric,
          ItemType = itemType,
          RequiredValue = requiredValue,
        },
        new QuestStepDefinition
        {
          Label = $"Talk to {character.Name}",
          Metric = QuestMetricKind.TalkedToNpc,
          ItemType = character.Id,
          RequiredValue = 1,
        },
      ],
    };
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
