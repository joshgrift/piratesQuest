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
  UniquePortsVisitedCount,
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
  public string GiverPortId { get; init; } = "";
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
  public int RewardGold { get; init; }
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

  private static readonly string FinishedTutorialQuestId = "scarlett_trade_for_merchant";

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
      CompletionText = null,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Move the ship once, with W, A, S, or D",
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
      AcceptedText = "Good. The ship listens to you, which puts you ahead of some captains already. Click and drag the camera once. A captain who never looks around is just volunteering to get jumped.",
      Description = "Scarlett wants you to stop staring straight ahead like a fresh deckhand. Click and drag to move your camera around once so you can actually watch the sea around you.",
      CompletionText = null,
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
      AcceptedText = "There ye go. A captain who can look around is much harder to surprise. Take the ship out with cannonballs aboard and fire one broadside. Q for port, E for starboard.",
      Description = "Scarlett wants you to fire your cannons once so you get used to broadside combat before the real work starts.",
      CompletionText = null,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Fire your cannons with Q or E once",
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
      AcceptedText = "Aye, that's the sound. You won't flinch the first time a fight starts now. A captain who can't arrive properly is just a floating apology. Press Tab to view the map, find a harbor, and set sail.",
      Description = "Scarlett wants proof that you can actually pull into a harbor without turning it into a scene. Sail to any port and dock once. Just move into the harbor interaction ring until the port panel opens.",
      CompletionText = null,
      Unlocks = [FeatureUnlock.SellGoods, FeatureUnlock.TavernTalk, FeatureUnlock.BuyGoods],
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Dock at any port. Look for a floating barrel",
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
      AcceptedText = "Aye, that's the sound. You won't flinch the first time a fight starts now. Gathering keeps you afloat, but trading makes you dangerous. Buy iron in the north at Rusthook Point or Haven Harbour, then sell it in the south at Tidefall Island or Spire Harbour for a profit. Earn 100 gold total while you're at it.",
      Description = "Scarlett wants one clean trade lesson instead of a dozen guesses. Buy iron somewhere cheap in the north, then sell it in a southern port for a profit. Rusthook Point and Haven Harbour are both good places to start, and Tidefall Island is the easiest first destination. Finish by earning 100 gold total.",
      CompletionText = "Better. Now you're trading with your head instead of your feelings. Now head back to port and find some more quests by talking to characters in the market. They might have some work for you, and they always have gossip that can point you toward good trade routes.",
      Unlocks = [FeatureUnlock.ShipyardComponents],
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Buy Iron in Rusthook Point or Haven Harbour",
          PreStepPopupText = "Northern ports like Rusthook Point and Haven Harbour sell iron cheaply.",
          Metric = QuestMetricKind.ItemsBought,
          ItemType = InventoryItemType.Iron.ToString(),
          RequiredValue = 1,
        },
        new QuestStepDefinition
        {
          Label = "Sell Iron for profit in a southern port",
          PreStepPopupText = "Take that iron south to Tidefall Island or Spire Harbour. Tidefall is the easiest early route, but both southern ports pay better than the northern mining towns.",
          Metric = QuestMetricKind.SoldProfit,
          ItemType = InventoryItemType.Iron.ToString(),
          RequiredValue = 1,
        },
        new QuestStepDefinition
        {
          Label = "Earn 100 gold",
          PreStepPopupText = "Repeat the iron run until you've earned 100 gold total.",
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
      GiverPortId = "saint-johns",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
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
      GiverPortId = "krakenfall",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
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
    new QuestDefinition
    {
      Id = "barnaby_round_trip",
      Title = "The Grand Port Tour",
      GiverNpcId = "barnaby-jape",
      GiverName = "Barnaby Jape",
      GiverPortId = "pebblehook-bay",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
      OfferText = "I have a very serious assignment for you: I need you to visit every port in the whole region so I can say I know a captain with stamina. Do it, come back taller in spirit, and I will pay you in glorious gold coin.",
      AcceptedText = "Off you go then. Make the rounds, wave at every dock, and try not to become local gossip in all eight places at once.",
      Description = "Barnaby wants you to sail to every port in the game at least once while this quest is active. It is a full tour of the map.",
      CompletionText = "Beautiful. Exhausting, impractical, and beautiful. As promised, here is a gold coin. Spend it with restraint.",
      RewardGold = 2,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Visit every port",
          Metric = QuestMetricKind.UniquePortsVisitedCount,
          RequiredValue = PortData.PortIds.Count,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "gideon_southern_iron_run",
      Title = "Southern Iron Run",
      GiverNpcId = "gideon-gearlock",
      GiverName = "Gideon Gearlock",
      GiverPortId = "saint-johns",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
      OfferText = "I have a modest contract for a captain who can follow numbers instead of moods. Take northern iron south and close three profitable iron sales, then I will get you and extra 250 gold.",
      AcceptedText = "Buy cheap iron in the north, sell it in the south, and do it profitably three times. Tidefall and Spire are both respectable destinations.",
      Description = "Gideon wants proof that your first lucky trade was not an accident. Sell Iron for profit after taking this quest. Northern ports are the best places to buy it, while southern ports usually pay better.",
      CompletionText = "Neat work. Predictable, profitable, and blessedly free of drama.",
      RewardGold = 250,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Sell Iron for profit",
          Metric = QuestMetricKind.SoldProfit,
          ItemType = InventoryItemType.Iron.ToString(),
          RequiredValue = 100,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "elsie_first_real_trade",
      Title = "Elsie's First Real Trade Tip",
      GiverNpcId = "elsie-drift",
      GiverName = "Elsie Drift",
      GiverPortId = "shard-bay",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
      OfferText = "I heard a route that sounds real, not tavern nonsense. If you try it and come back richer, could you tell me I was not foolish for noticing it?",
      AcceptedText = "Fish is cheap around here. Sell it somewhere that actually misses it and come back with proof you made the right call.",
      Description = "Elsie wants help proving that the route she overheard is actually good. Sell Fish for profit 2 times after accepting this quest.",
      CompletionText = "It worked? Good. I mean, of course it worked. I was nearly certain.",
      RewardGold = 400,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Sell Fish for profit",
          Metric = QuestMetricKind.SoldProfit,
          ItemType = InventoryItemType.Fish.ToString(),
          RequiredValue = 100,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "merrick_fill_the_hold",
      Title = "Fill the Hold with Timber",
      GiverNpcId = "merrick-ash",
      GiverName = "Merrick Ash",
      GiverPortId = "tidefall-island",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
      OfferText = "You want easy coin? Bring back a proper load instead of three apologetic planks. Fill the hold with wood and I will pay you 4000 gold.",
      AcceptedText = "Bring in 800 wood. Cut it, stack it, move it. Then come collect your pay.",
      Description = "Merrick is offering quick cash for a solid timber run. Collect 800 Wood after accepting this quest.",
      CompletionText = "That is a real haul. You brought wood, not excuses.",
      RewardGold = 4000,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Collect 800 Wood",
          Metric = QuestMetricKind.ItemsCollected,
          ItemType = InventoryItemType.Wood.ToString(),
          RequiredValue = 800,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "rafael_fresh_catch",
      Title = "Fresh Catch Bonus",
      GiverNpcId = "rafael-tide",
      GiverName = "Rafael Tide",
      GiverPortId = "tidefall-island",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
      OfferText = "Bring me proof you can work the water cleanly and I will make it worth the trouble. I need fish, not stories. I'll pay you a gold for each fish you bring me.",
      AcceptedText = "Catch 160 fish and bring back a proper haul. If the sea likes you today, so will I.",
      Description = "Rafael is paying captains who can put together a real fishing run. Collect 160 Fish after accepting this quest.",
      CompletionText = "That is a catch worth admiring. Nicely done.",
      RewardGold = 160,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Collect 160 Fish",
          Metric = QuestMetricKind.ItemsCollected,
          ItemType = InventoryItemType.Fish.ToString(),
          RequiredValue = 160,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "barnaby_delivery_for_elsie",
      Title = "Barnaby's Sealed Note",
      GiverNpcId = "barnaby-jape",
      GiverName = "Barnaby Jape",
      GiverPortId = "pebblehook-bay",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
      OfferText = "I have an extremely important note for Elsie Drift in Shard Bay. It may be heartfelt, embarrassing, or both. Deliver it for me and I shall reward your heroism handsomely with 1000 gold coins.",
      AcceptedText = "Take my sealed note to Elsie Drift in Shard Bay. Do try not to read it unless the temptation becomes artistically overwhelming.",
      Description = "Barnaby wants you to deliver a package to Elsie Drift in Shard Bay. Just reach Elsie and speak with her while this quest is active.",
      CompletionText = "Marvelous. The note arrived, Elsie endured it, and you are richer for your service.",
      RewardGold = 1000,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Deliver Barnaby's package to Elsie Drift",
          Metric = QuestMetricKind.TalkedToNpc,
          ItemType = "elsie-drift",
          RequiredValue = 1,
        },
      ],
    },
    new QuestDefinition
    {
      Id = "valora_packet_for_caspian",
      Title = "A Quiet Packet",
      GiverNpcId = "valora-rumwhisper",
      GiverName = "Valora Rumwhisper",
      GiverPortId = "krakenfall",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
      OfferText = "Governor Caspian pretends to dislike my little errands, which only makes them more amusing. Take this packet to him in Rusthook Point and do try to look innocent while doing it. I'll pay you 850 gold.",
      AcceptedText = "Carry my packet north to Governor Caspian in Rusthook Point. If he sighs before opening it, tell him I consider that affection.",
      Description = "Valora wants you to deliver a package to Governor Caspian in Rusthook Point. Just talk to him there while this quest is active.",
      CompletionText = "Perfect. He received it, which means he will now spend the evening pretending he did not.",
      RewardGold = 850,
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Deliver Valora's packet to Governor Caspian",
          Metric = QuestMetricKind.TalkedToNpc,
          ItemType = "governor-caspian",
          RequiredValue = 1,
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
    new QuestDefinition
    {
      Id = "hire_harlan_bentbeam",
      Title = "Earn Harlan Bentbeam's Trust",
      GiverNpcId = "harlan-bentbeam",
      GiverName = "Harlan Bentbeam",
      GiverPortId = "krakenfall",
      PrerequisiteQuestIds = [FinishedTutorialQuestId],
      Repeatable = true,
      RewardCrewNpcId = "harlan-bentbeam",
      OfferText = "I keep hulls knitting themselves back together, but only for captains who respect the timber and the finer things in life. Bring back 200 wood and 50 tea after honest work, then talk to me and prove you know what keeps a ship alive.",
      AcceptedText = "Bring me 200 wood and 50 tea, then come speak with me again. Show me you can keep good material moving and I will keep your hull mending between fights.",
      Description = "Harlan improves your hull regeneration, but he wants to see that you understand the value of solid timber and tea stores. Collect 200 Wood and 50 Tea after accepting this quest, then return to Harlan in Krakenfall and talk to him to recruit him.",
      CompletionText = "Suitable. I will join your crew, and your hull will start recovering itself more reliably.",
      Steps =
      [
        new QuestStepDefinition
        {
          Label = "Collect 200 Wood",
          Metric = QuestMetricKind.ItemsCollected,
          ItemType = nameof(InventoryItemType.Wood),
          RequiredValue = 200,
        },
        new QuestStepDefinition
        {
          Label = "Collect 50 Tea",
          Metric = QuestMetricKind.ItemsCollected,
          ItemType = nameof(InventoryItemType.Tea),
          RequiredValue = 50,
        },
        new QuestStepDefinition
        {
          Label = "Talk to Harlan Bentbeam",
          Metric = QuestMetricKind.TalkedToNpc,
          ItemType = "harlan-bentbeam",
          RequiredValue = 1,
        },
      ],
    },
    CreateHireQuest(
      "hire_merrick_ash",
      "merrick-ash",
      "Collect 12 Wood",
      QuestMetricKind.ItemsCollected,
      itemType: nameof(InventoryItemType.Wood),
      requiredValue: 12,
      offerText: "I can speed up your wood hauls, but I do not work for captains who only admire trees from the deck. Bring me proof with 12 wood in the hold, then come talk to me and show me you can finish a proper run.",
      acceptedText: "Load up 12 wood, then return and speak with me. If you can run timber without wasting daylight, I will make every future haul better.",
      description: "Merrick improves wood collection, but he only signs on after seeing a real lumber run. Collect 12 Wood after accepting this quest, then return to Merrick in Haven Harbour and talk to him to recruit him.",
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
      description: "Rafael improves fish collection, but he wants proof that you can actually work the water. Collect 10 Fish after accepting this quest, then return to Rafael in Haven Harbour and talk to him to recruit him.",
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
      description: "Silas improves iron collection, but he only joins captains who can bring ore home the hard way. Collect 10 Iron after accepting this quest, then return to Silas in Haven Harbour and talk to him to recruit him.",
      completionText: "That is solid work. I will join your crew, and your mining trips will start paying out better."
    ),
    CreateHireQuest(
      "hire_nera_quicksnap",
      "nera-quicksnap",
      "Visit 3 unique ports",
      QuestMetricKind.UniquePortsVisitedCount,
      requiredValue: 3,
      offerText: "You want me aboard? Fine. Prove your ship can move. Visit three different ports after you accept this, then come back with a little speed in your wake.",
      acceptedText: "Three unique ports. No excuses, no circling the same dock, and no pretending a fast launch matters if you never leave harbor.",
      description: "Nera boosts your acceleration slightly, but only after you show her you can keep moving. Visit 3 different ports after accepting her quest, then return to Nera in Haven Harbour and talk to her to recruit her.",
      completionText: "That is better. You kept moving, so I will make sure your ship does too."
    ),
    CreateHireQuest(
      "hire_vera_vane",
      "vera-vane",
      "Collect 100 Wood",
      QuestMetricKind.ItemsCollected,
      requiredValue: 100,
      offerText: "Your hull is begging for a finer hand. Bring me 100 wood after you accept this offer and I will show you what real structure looks like.",
      acceptedText: "Go gather 100 wood for me, captain. Then come back and let me improve your ship properly.",
      description: "Collect 100 Wood after accepting Vera's quest, then return to Vera in Spire Harbour and talk to her to recruit her.",
      completionText: "Wonderful. That timber will do nicely, and I am sure you will feel the difference soon enough.",
      itemType: nameof(InventoryItemType.Wood)
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
    return PortData.GetCharacterById(npcId)?.Portrait ?? "";
  }

  public static QuestDefinition[] GetAvailableQuests(
    IEnumerable<string> completedQuestIds,
    string currentQuestId,
    IEnumerable<string> hiredCrewIds = null)
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
    var character = PortData.GetCharacterById(characterId);
    if (character == null)
      throw new InvalidOperationException($"Cannot create hire quest for missing character '{characterId}'.");

    return new QuestDefinition
    {
      Id = id,
      Title = $"Earn {character.Name}'s Trust",
      GiverNpcId = character.Id,
      GiverName = character.Name,
      GiverPortId = PortData.GetPortIdForCharacter(character.Id) ?? "",
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
