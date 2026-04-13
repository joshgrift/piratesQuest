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
    // Saint Johns is a lumber-heavy starter port. Based in the centre of the map.
    // It exports wood cheaply, but it pays a premium for tea hauled in from the tea ports.
    new()
    {
      Id = "saint-johns",
      DisplayName = "Saint Johns",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 4, SellPrice = 2 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 10, SellPrice = 8 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 5, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 14, SellPrice = 11 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 51, SellPrice = 42 },
      ],
      CharacterIds = ["gideon-gearlock", "elder-bertram"],
    },
    // Rusthook Point is a northern mining port.
    // Iron is a reliable export, and nearby demand is intentionally weaker than the distant routes.
    new()
    {
      Id = "rusthook-point",
      DisplayName = "Rusthook Point",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 10, SellPrice = 8 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 6, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 4, SellPrice = 2 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 10, SellPrice = 7 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 54, SellPrice = 45 },
      ],
      CharacterIds = ["tommy-fuse", "governor-caspian"],
    },
    // Shard Bay sits close to the center, so its routes are convenient instead of exceptional.
    // Fish are cheap here, while tea costs more because it has to be shipped in from the tea ports. It also produces iron.
    new()
    {
      Id = "shard-bay",
      DisplayName = "Shard Bay",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 8, SellPrice = 6 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 7, SellPrice = 4 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 1 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 11, SellPrice = 8 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 55, SellPrice = 46 },
      ],
      CharacterIds = ["dorian-blackwake", "elsie-drift"],
    },
    // Krakenfall is one of the two ports nearest the tea fields.
    // Tea should be easy to buy here, then worth carrying back across the map.
    new()
    {
      Id = "krakenfall",
      DisplayName = "Krakenfall",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 8, SellPrice = 6 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 7, SellPrice = 6 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 9, SellPrice = 6 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 44, SellPrice = 36 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 7, SellPrice = 4 },
      ],
      CharacterIds = ["valora-rumwhisper", "harlan-bentbeam"],
    },
    // Tidefall Island is the other port nearest the tea fields.
    // It offers the cheapest tea and pays one of the best iron prices in the game.
    new()
    {
      Id = "tidefall-island",
      DisplayName = "Tidefall Island",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 4, SellPrice = 2 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 4, SellPrice = 2 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 13, SellPrice = 10 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 48, SellPrice = 39 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 6, SellPrice = 4 },
      ],
      CharacterIds = ["merrick-ash", "rafael-tide"],
    },
    // Haven Harbour is a northern industrial stop.
    // It exports iron and fish, but its tea demand helps create valuable long-haul runs.
    new()
    {
      Id = "haven-harbour",
      DisplayName = "Haven Harbour",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 10, SellPrice = 8 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 3, SellPrice = 1 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 6, SellPrice = 4 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 52, SellPrice = 44 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 12, SellPrice = 10 },
      ],
      CharacterIds = ["silas-quill", "nera-quicksnap"],
    },
    // Pebblehook Bay is a far-west destination for long-haul tea runs.
    // Local fish and wood stay cheap, but imported luxury goods should sell much better here.
    new()
    {
      Id = "pebblehook-bay",
      DisplayName = "Pebblehook Bay",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 5, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 2, SellPrice = 1 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 11, SellPrice = 8 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 54, SellPrice = 45 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 14, SellPrice = 11 },
      ],
      CharacterIds = ["barnaby-jape"],
    },
    // Spire Harbour is a southern, eastern long-haul trade partner.
    // It sells wood and ammunition cheaply, then pays well for iron, and tea.
    new()
    {
      Id = "spire-harbour",
      DisplayName = "Spire Harbour",
      ItemsForSale =
      [
        new() { ItemType = InventoryItemType.Wood, BuyPrice = 5, SellPrice = 3 },
        new() { ItemType = InventoryItemType.Fish, BuyPrice = 7, SellPrice = 5 },
        new() { ItemType = InventoryItemType.Iron, BuyPrice = 12, SellPrice = 9 },
        new() { ItemType = InventoryItemType.CannonBall, BuyPrice = 43, SellPrice = 35 },
        new() { ItemType = InventoryItemType.Tea, BuyPrice = 13, SellPrice = 10 },
      ],
      CharacterIds = ["vera-vane"],
    },
  ];

  private static readonly PortNpcDefinition[] _characters =
  [
    // Scarlett is the player's blunt first mate. She has survived bad captains, bad deals,
    // and bad weather, so every line she speaks is shaped like a practical lesson with just a hint of teasing charm for the captain.
    new()
    {
      Id = ScarlettId,
      Name = "Scarlett",
      Role = "First Mate",
      Portrait = "character2.png",
      Hireable = false,
      TalkPhrases =
      [
        "Keep your eyes moving, captain. I'd hate to lose a face that pretty to something as dull as tunnel vision.",
        "If a job feels too safe, you probably missed the dangerous part. Try to stay sharp for me.",
        "Quests teach the ropes, but habits keep you alive after the lesson ends, and I am getting rather used to having you around.",
        "Ports are for selling, repairing, and making smarter mistakes than the last voyage. Preferably fewer of the dramatic kind.",
        "Do not confuse confidence with steering in a straight line toward trouble. Swagger is only charming when you survive it.",
        "A calm captain makes fewer repairs and earns fewer enemies. Very attractive qualities, as it happens.",
        "If you leave port without a plan, at least leave with cannonballs. I can work with reckless, but not unprepared.",
        "The map is not decoration, love. Use it before the sea decides to flirt back with teeth.",
        "Gold is only useful if you live long enough to spend it, so do try to make it back to me in one piece.",
        "Every good crew starts with a captain willing to listen once in a while. Lucky for you, I enjoy repeating myself.",
      ],
    },
    // Gideon is a polished broker who grew up around ledgers, shortages, and dockside auctions.
    // He sounds refined, but underneath that coat he is still a shark who smells weak pricing instantly.
    new()
    {
      Id = "gideon-gearlock",
      Name = "Gideon Gearlock",
      Role = "Merchant Broker",
      Portrait = "character8.png",
      Hireable = true,
      TalkPhrases =
      [
        "Tea from the southern tea ports sells beautifully in western Saint Johns if you can keep your cargo dry.",
        "Northern Rusthook iron carried south to Tidefall is not glamorous, but it is dependable, and dependable is how fortunes begin.",
        "Southern ports pay more for iron than northern mining towns ever will. The map rewards captains who notice that.",
        "Fish from Pebblehook or Shard Bay do very nicely in Krakenfall, assuming you arrive before the smell announces you.",
        "Tea from southern Tidefall to far-west Pebblehook is a longer sail, but the ledger forgives the distance rather handsomely.",
        "If you leave northern Haven with cheap iron, aim south for Tidefall. If you leave southern Tidefall with cheap tea, aim west for Saint Johns or Pebblehook.",
        "Southern Spire pays well for iron and tea, but Tidefall is often the easier first stop for new captains.",
        "Shard Bay fish are common there and precious elsewhere. That difference is where your gold lives.",
        "Cheap goods are a promise. Good routes are the part that keeps the promise.",
        "Most fortunes begin with someone noticing the same numbers everyone else ignored.",
        "I like captains who understand that gold and timing are cousins.",
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
    // Tommy is a powder runner who treats every gun deck like a stage and every broadside like applause.
    // He jokes often, but his obsession with clean firing drills comes from surviving ships that lacked them.
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
        "A broadside should feel rehearsed even when the fight is not.",
        "If your deckhands are running in circles, the enemy already won half the argument.",
        "Good gunners respect distance before they start respecting noise.",
        "You can tell a lot about a captain by how they reload after the first shot.",
        "I like a ship that smells a little like smoke and a lot like confidence.",
        "Misses are expensive. Dramatic misses are just embarrassing.",
        "The trick is not loving cannons. The trick is loving discipline more than cannons.",
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
    // Elder Bertram spent decades keeping ugly ships afloat and arrogant captains alive.
    // He speaks like a tired craftsman because he is one, but he still cannot resist fixing bad work.
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
        "If the frame is honest, the sea has to work harder to ruin your day.",
        "Fresh planks are good. Proper joins are better.",
        "I have seen captains decorate a ship before learning how to strengthen it. Sad business.",
        "Hull work is slow because panic is terrible carpentry.",
        "The sea does not care how expensive your mistakes were.",
        "A shipwright's pride is hearing nothing break when it should have.",
        "You can tell whether a vessel was loved by how it survives the first bad week.",
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
    // Dorian is a once-feared cannoneer who survived more duels than his temper should have allowed.
    // He is harsh because he has watched hesitation kill crews that should have known better.
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
        "The best shot is the one you lined up before fear got involved.",
        "Do not chase glory. Chase angles, reloads, and exits.",
        "A damaged ship can still win. A doubtful crew usually cannot.",
        "Most battles are lost in the captain's head before the hull gives up.",
        "I prefer clean endings. Sunk ships do not come back with excuses.",
        "If you fire because you are angry, you are already aiming poorly.",
        "I do not need bravery from a crew. I need obedience when the smoke rolls in.",
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
    // Valora deals in rumors because gossip kept her richer than honest labor ever did.
    // She enjoys sounding mysterious, but most of her advice comes from relentless eavesdropping.
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
        "Krakenfall watches black powder more closely than most people realize, especially when Tommy Fuse starts grinning.",
        "Buy a rumor before you buy trouble. It's usually cheaper.",
        "People whisper that Gideon Gearlock can smell a bad price before the market board is painted.",
        "Dorian Blackwake claims he hates conversation, yet half the harbor repeats his insults by sundown.",
        "Nothing spreads faster than news that Barnaby Jape has sent another captain on a foolish errand.",
        "I hear Nera Quicksnap judges a ship by the first breath it takes leaving dock, and she is rarely kind.",
        "Governor Caspian pretends not to listen in taverns. That only works on people who have never watched him.",
        "If Vera Vane offers to improve your hull, make certain you enjoy surprises more than safety.",
        "Scarlett's name still travels ahead of her. Useful woman to know, dangerous woman to disappoint.",
      ],
    },
    // Harlan learned repair work in storm country where weak patch jobs meant funerals.
    // He is patient, practical, and deeply offended by lazy maintenance.
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
        "I like captains who notice groaning timbers before they notice incoming fire.",
        "A ship heals better when the crew stops treating every scrape like a surprise.",
        "If you patch in a hurry, you sail in a hurry too.",
        "Timber remembers stress even when captains pretend not to.",
        "Hull work is mostly respect, repetition, and refusing shortcuts.",
        "There is no romance in maintenance, which is why it saves so many lives.",
        "Bring me good wood and a little peace and I can buy you time in a fight.",
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
    // Governor Caspian is an administrator pretending to be relaxed while juggling ten emergencies.
    // He talks like a statesman, but years of crisis management have given him a dry pirate streak.
    new()
    {
      Id = "governor-caspian",
      Name = "Governor Caspian Vale",
      Role = "Acting Governor",
      Portrait = "character18.png",
      Hireable = false,
      TalkPhrases =
      [
        "Northern Rusthook iron sold in southern Tidefall is the kind of route even a governor can admire: simple, legal, and profitable.",
        "Northern Haven Harbour's iron also travels well to southern Tidefall, though fewer captains seem organized enough to notice.",
        "Southern ports pay more for iron than the northern producers do, which is exactly how trade is supposed to work.",
        "Pebblehook fish sold in Krakenfall have funded more repairs than some tax policies.",
        "Tea from the southern tea ports to western Saint Johns pays well enough that I notice whenever captains stop running it.",
        "Tea from southern Tidefall to far-west Pebblehook is longer, riskier, and exactly the sort of route that rewards competent planning.",
        "Spire Harbour pays visiting captains very kindly for iron and tea, though Tidefall usually gets the first wave of sensible traders.",
        "Shard Bay's fish are cheap because they are everywhere there. They are not everywhere once you sail away from Shard Bay.",
        "A calm ledger is usually a sign that someone has stopped reporting honestly. A lively one often means someone found a route first.",
        "I do not dislike pirates. I dislike pirates who discover profitable routes and then turn them into incidents.",
      ],
    },
    // Merrick is a no-nonsense lumberman who trusts work more than charm.
    // Years in the forests left him blunt, strong, and unimpressed by anyone who wastes daylight.
    new()
    {
      Id = "merrick-ash",
      Name = "Merrick Ash",
      Role = "Lumberjack",
      Portrait = "character25.png",
      Hireable = true,
      TalkPhrases =
      [
        "Wood runs go bad when people wander and call it teamwork. Load up here or in Saint Johns, then sell at Spire if you want the trip to pay.",
        "Cut, stack, move. The trees don't care about excuses.",
        "A clean haul beats a loud one.",
        "Most axes fail in the hands before they fail at the edge.",
        "If you want more lumber, stop standing around admiring the forest.",
        "Timber work is simple. The people around timber work are usually the hard part.",
        "A captain who respects cargo gets more of it home.",
        "You can learn a lot about a crew from how they lift together.",
        "I trust habits more than promises and sharp tools more than speeches.",
        "The best lumber run ends with fewer stories and a fuller hold.",
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
    // Rafael treats fishing like music and weather like a conversation.
    // He is charming because it helps, but he survives by reading water better than most sailors read maps.
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
        "Most crews fish where it's easy, then wonder why supper feels insulting. Fish from Shard Bay or Pebblehook fetch much better coin in Krakenfall.",
        "Charm helps, but timing lands the catch.",
        "The sea gives more to crews who arrive quietly and leave quickly.",
        "A net thrown badly is just a public confession.",
        "Fish move like gossip. Follow the sudden shifts.",
        "You can waste an hour fighting water that was trying to teach you a shortcut.",
        "Good fishing crews look lazy right before the hold fills up.",
        "Patience matters, but position matters first.",
        "I like captains who understand that feeding a crew is half of winning any voyage.",
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
    // Silas is a sculptor who drifted into mining because stone pays more reliably than admiration.
    // He values rhythm, patience, and precise work, whether he is shaping statues or cracking ore.
    new()
    {
      Id = "silas-quill",
      Name = "Silas Quill",
      Role = "Stone Sculptor",
      Portrait = "character23.png",
      Hireable = true,
      TalkPhrases =
      [
        "Stone rewards rhythm. Swing sloppy and the island laughs at you. And if you haul that iron to Tidefall, the market laughs a lot less.",
        "Good mining looks patient right up until the hold starts filling fast.",
        "Iron comes easier when the crew stops fighting the rock.",
        "Every strike should have a reason. Noise is not a reason.",
        "The best miners remove exactly what they meant to and nothing proud besides.",
        "Ore veins have moods. Learn them and the work becomes kinder.",
        "There is elegance in a clean break and profit in repeating it.",
        "Rushing stone only proves that flesh is softer.",
        "A good haul begins with someone noticing the line in the rock everyone else ignored.",
        "I prefer tools with weight, crews with patience, and captains who do not shout underground.",
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
    // Nera is a sharp-tongued racer who spent years humiliating slower captains in harbor runs.
    // She loves speed, hates hesitation, and treats every launch like a chance to prove a point.
    new()
    {
      Id = "nera-quicksnap",
      Name = "Nera Quicksnap",
      Role = "Dockside Racer",
      Portrait = "character30.png",
      Hireable = true,
      TalkPhrases =
      [
        "You turn like you're asking permission from the wind.",
        "Fast starts win races and save skins. Usually both.",
        "A good launch should feel rude to anyone watching from the dock.",
        "You can learn a lot about a captain from the first three seconds after undocking.",
        "If the ship hesitates, fix the hands before you blame the hull.",
        "Momentum is a mood, captain. Try having one.",
        "I adore ships that leap forward before fear catches them.",
        "Most people call me impatient when they mean correct.",
        "There is nothing graceful about being slow on purpose.",
        "Get me aboard and I will shave the doubt right out of your acceleration.",
      ],
      HireText = "Hire me and I will make your ship answer faster. Try to keep up with your own decisions.",
      FireText = "Fine. Drift out of port dramatically without me, then.",
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipAcceleration,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = 0.4f,
        },
      ],
    },
    // Elsie grew up unwanted, bullied, and told she would never be chosen for anything important.
    // She is timid on the surface, but underneath that fear is a stubborn need to prove she belongs.
    new()
    {
      Id = "elsie-drift",
      Name = "Elsie Drift",
      Role = "Deckhand Hopeful",
      Portrait = "character16.png",
      Hireable = false,
      TalkPhrases =
      [
        "I know I am not impressive yet. I am working on the yet part.",
        "People keep saying I am not built for sea work. I would like to prove them tired. The smart ones buy fish in Shard Bay, then head for Krakenfall before the hold starts to smell.",
        "I watch good crews a lot. It is easier than joining them. Some of them buy tea in the south near Tidefall or Krakenfall and bring it all the way back to western Saint Johns.",
        "One captain told me far-west Pebblehook pays even better for southern Tidefall tea, but only if you are brave enough for the longer sail.",
        "I heard a crew in here bragging that northern Rusthook iron becomes real money the moment it reaches southern Tidefall, and Spire is not far behind.",
        "Some sailors say southern ports pay more for iron. They sounded very sure of themselves, which usually means they were right for once.",
        "Nobody listens to deckhands much, so I hear things. Fish from Pebblehook sell well in Krakenfall too.",
        "I am not asking to be special. I am asking for a chance to get good. Watching routes helps with that.",
        "I have practiced knots until my hands ached just so nobody can call me useless twice.",
        "Sometimes surviving long enough to try again is its own sort of victory.",
        "If I ever make it, I think I will be kinder than the people who laughed first.",
      ],
      HireText = "",
      FireText = "",
      StatChanges = [],
    },
    // Barnaby is a cheerful tormentor who collects stories, dares, and other people's inconvenience.
    // He means well in his own annoying way, and he thinks a joke is better when it sends someone across the map.
    new()
    {
      Id = "barnaby-jape",
      Name = "Barnaby Jape",
      Role = "Jolly Fellow",
      Portrait = "character4.png",
      Hireable = false,
      TalkPhrases =
      [
        "You look like a captain who could use a completely unnecessary adventure.",
        "I respect ambition most when it is mildly inconvenient.",
        "Travel broadens the mind and wears out the boots of everyone involved.",
        "If you visit every port, you can complain about all of them with authority.",
        "A proper jest leaves someone with a story and a little sea spray.",
        "People call me a tease, which is unfair. I am also very charming.",
        "You would be amazed how many great decisions begin as obviously bad ideas.",
        "I like maps because they are just dares drawn neatly.",
        "One gold is a terrible reward, which makes it unforgettable.",
        "A captain who cannot laugh at a quest like mine is probably no fun in a storm either.",
      ],
    },
    // Vera is Scarlett's twin sister, but where Scarlett became hard and protective,
    // Vera became venomous, theatrical, and delighted by slow sabotage dressed as advice.
    new()
    {
      Id = "vera-vane",
      Name = "Vera Vane",
      Role = "Hull Specialist",
      Portrait = "character24.png",
      Hireable = true,
      TalkPhrases =
      [
        "Scarlett always did love saving hopeless captains. I prefer studying how they break.",
        "Your ship has potential, which is another way of saying it disappoints me today.",
        "I could help you, in the same way a storm helps a weak mast reveal itself. Buy tea in Saint Johns and sell it at Spire, why don't you.",
        "My sister thinks strength is kindness with a sword. How provincial.",
        "You only need a small adjustment to your hull. A very memorable one.",
        "I adore captains who trust the wrong woman twice.",
        "Nothing ruins Scarlett's day like seeing someone choose me instead.",
        "Destruction is just improvement that stopped pretending to care.",
        "The sea rewards honesty. I am honest enough to admit I enjoy the damage.",
        "Come closer, captain. Sabotage is such an ugly word for a professional opinion.",
      ],
      HireText = "I can refine your ship once you have handled enough wood to appreciate craftsmanship. Do let me help.",
      FireText = "How tragic. I was only beginning to make an impression.",
      StatChanges =
      [
        new RecordPlayerStatChange
        {
          Stat = PlayerStat.ShipHullStrength,
          Modifier = RecordPlayerStatChangeModifier.Additive,
          Value = -20.0f,
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
