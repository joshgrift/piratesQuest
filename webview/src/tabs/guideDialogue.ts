// Scarlett's dialogue tree — the branching conversation data used by GuideTab.
// Kept in a separate file so GuideTab only exports its React component
// (required by react-refresh for hot module replacement).

export interface DialogueNode {
  text: string;
  responses: { label: string; next: string }[];
}

export const GUIDE_DIALOGUE: Record<string, DialogueNode> = {
  root: {
    text: "Ahoy there, sailor! Pull up a chair. Name's Scarlett \u2014 been sailin' these waters longer than most. What would ye like to know?",
    responses: [
      { label: "How do I sail my ship?", next: "sailing" },
      { label: "How does trading work?", next: "trading" },
      { label: "Tell me about combat", next: "combat" },
      { label: "What are resources for?", next: "resources" },
      { label: "How do I collect resources?", next: "collecting" },
      { label: "How do ship upgrades work?", next: "upgrades" },
      { label: "How do I upgrade my ship class?", next: "ship_tiers" },
      { label: "What if I'm overburdened?", next: "overburdened" },
      { label: "How does the leaderboard work?", next: "leaderboard" },
      { label: "What happens when I die?", next: "death" },
      { label: "What can I do at ports?", next: "ports" },
      { label: "How does the vault work?", next: "vault" },
    ],
  },

  // ── Sailing ──
  sailing: {
    text: "Sailin' is simple, love. W moves ye forward, S slows ye down, and A/D turns the ship. She's got momentum though \u2014 plan yer turns early or you'll be kissin' the shoreline!",
    responses: [
      { label: "Any sailing tips?", next: "sailing_tips" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  sailing_tips: {
    text: "Here's a free one: a loaded ship handles worse. The heavier yer cargo, the slower ye turn. You'll see a 'Heavy' warning when near capacity.\n\nQuick quiz \u2014 when yer ship is heavy with cargo, what happens?",
    responses: [
      { label: "It turns slower", next: "sailing_right" },
      { label: "It goes faster", next: "sailing_wrong" },
      { label: "Nothing changes", next: "sailing_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  sailing_right: {
    text: "Sharp as a cutlass! Heavy ships handle like a drunken whale. Keep that in mind when ye're loaded with trade goods heading through tight waters!",
    responses: [{ label: "What else can I learn?", next: "root" }],
  },
  sailing_wrong: {
    text: "Not quite, love! A heavy ship turns slower and handles worse. Ye'll see the 'Heavy' icon on screen when near capacity. Sell off some goods or choose yer route carefully!",
    responses: [{ label: "Good to know! What else?", next: "root" }],
  },

  // ── Trading ──
  trading: {
    text: "Now ye're speakin' my language! Each port has different prices for goods. The secret? Buy cheap at one port, sail across the map, and sell dear at another. Supply and demand, sailor!",
    responses: [
      { label: "How do I buy and sell?", next: "trading_how" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  trading_how: {
    text: "When ye dock, check the Market tab \u2014 right here! Switch between 'Buy Goods' and 'Sell Goods' at the top, set yer quantities, and confirm. Simple as breathin'!\n\nNow tell me \u2014 to make the most gold, what should ye do?",
    responses: [
      { label: "Buy low at one port, sell high at another", next: "trading_right" },
      { label: "Sell everything at the first port", next: "trading_wrong" },
      { label: "Only collect, never buy", next: "trading_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  trading_right: {
    text: "Ha! Ye'll be a merchant prince in no time! Every port has different prices \u2014 compare before ye sell. Gold is king, sailor. It buys components and puts ye on the leaderboard!",
    responses: [{ label: "What else can I learn?", next: "root" }],
  },
  trading_wrong: {
    text: "Bless yer heart, no! The trick is buyin' where it's cheap and sellin' where it's dear. Every port has different prices \u2014 always check before ye unload!",
    responses: [{ label: "I'll remember that! What else?", next: "root" }],
  },

  // ── Combat ──
  combat: {
    text: "Time to talk firepower! Press Q to fire yer port-side cannons (that's left), and E for starboard (right). Each shot uses a cannonball from yer hold, with a 2-second cooldown between volleys.",
    responses: [
      { label: "What can I fight?", next: "combat_targets" },
      { label: "Any combat tips?", next: "combat_tips" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  combat_targets: {
    text: "Other players are the biggest prize \u2014 sink one and ye get half their inventory! But they're thinkin' the same about you, so stay sharp out there.",
    responses: [
      { label: "Any combat tips?", next: "combat_tips" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  combat_tips: {
    text: "Always keep cannonballs stocked! Nothin' worse than an empty cannon in a fight. And remember \u2014 ports are safe zones. If things go south, run for shore!\n\nPop quiz, sailor! Which key fires yer LEFT cannons?",
    responses: [
      { label: "Q", next: "combat_right" },
      { label: "E", next: "combat_wrong" },
      { label: "Space", next: "combat_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  combat_right: {
    text: "That's it! Q for port, E for starboard. A true sailor always knows their port from their starboard. Now get out there and give 'em hell!",
    responses: [{ label: "What else should I know?", next: "root" }],
  },
  combat_wrong: {
    text: "Almost! Q fires port-side (left), E fires starboard (right). Easy to remember: Q is on the left of yer keyboard \u2014 just like port side!",
    responses: [{ label: "Got it! What else?", next: "root" }],
  },

  // ── Collecting ──
  collecting: {
    text: "See those glowing spots out on the water? Those are collection points! Each one gives a different resource \u2014 Wood, Iron, Fish, or Tea. Just sail yer ship close and yer crew handles the rest.",
    responses: [
      { label: "How does it work exactly?", next: "collecting_how" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  collecting_how: {
    text: "When ye enter a collection point, you'll see a progress indicator on screen. Stay inside the ring and resources flow into yer hold automatically. The longer ye stay, the more ye collect!\n\nBut watch yer cargo capacity \u2014 a full hold means ye can't gather more. Sell or spend yer goods to make room.",
    responses: [
      { label: "Can I collect faster?", next: "collecting_upgrades" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  collecting_upgrades: {
    text: "Aye! There are ship components that boost yer collection rates by 50% \u2014 Advanced Fish Nets, Reinforced Lumber Tools, and Enhanced Mining Tools. Buy 'em in the Shipyard tab and equip 'em before ye head out.\n\nAlso, the Expanded Cargo Hold gives ye more room to carry what ye gather. A smart sailor upgrades before they harvest!",
    responses: [{ label: "Good to know! What else?", next: "root" }],
  },

  // ── Resources ──
  resources: {
    text: "Resources are the lifeblood of yer journey! Sail near the glowing collection points scattered around the map and yer crew will start gatherin' automatically.",
    responses: [
      { label: "What resources are there?", next: "resources_list" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  resources_list: {
    text: "Six things ye need to know:\n\n\u2022 Wood \u2014 repairs, crafting, trading\n\u2022 Iron \u2014 components, trading\n\u2022 Fish \u2014 repairs, trading\n\u2022 Tea \u2014 valuable trade good\n\u2022 Gold \u2014 the universal currency\n\u2022 Cannonballs \u2014 don't leave port without 'em!\n\nNow, what do ye need to repair yer ship?",
    responses: [
      { label: "Wood and Fish", next: "resources_right" },
      { label: "Just Gold", next: "resources_wrong" },
      { label: "Iron and Tea", next: "resources_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  resources_right: {
    text: "Aye! 5 Wood and 1 Fish per point of hull ye repair. Always keep some in reserve \u2014 ye never know when you'll limp into port full of holes!",
    responses: [{ label: "Good tip! What else?", next: "root" }],
  },
  resources_wrong: {
    text: "Not quite! Ye need Wood and Fish \u2014 5 Wood and 1 Fish per hull point. Head to the Shipyard tab to repair. Ye can also buy materials with Gold if you're short!",
    responses: [{ label: "I'll stock up! What else?", next: "root" }],
  },

  // ── Upgrades ──
  upgrades: {
    text: "Ship components are what separate a floatin' plank from a war vessel! Buy 'em in the Shipyard tab with resources, then equip 'em to boost yer stats \u2014 speed, damage, cargo space, and more.",
    responses: [
      { label: "How do I equip them?", next: "upgrades_equip" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  upgrades_equip: {
    text: "Head to the Shipyard tab when docked. Components for sale are at the bottom \u2014 buy one, then equip it from yer owned list. Ye've got limited slots, so choose wisely!\n\nImportant question \u2014 what happens to yer components when ye die?",
    responses: [
      { label: "They're all lost", next: "upgrades_right" },
      { label: "They stay equipped", next: "upgrades_wrong" },
      { label: "Half are lost", next: "upgrades_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  upgrades_right: {
    text: "Aye... the harsh truth of the sea. Every equipped component is gone when ye sink. Smart sailors only invest heavy when they can defend themselves!",
    responses: [{ label: "That's rough! What else?", next: "root" }],
  },
  upgrades_wrong: {
    text: "I wish, love! When ye die, ALL equipped components are lost forever. It's a cruel sea \u2014 only load up on upgrades when ye've got the firepower to keep 'em!",
    responses: [
      { label: "What about upgrading my ship class?", next: "ship_tiers" },
      { label: "I'll be careful! What else?", next: "root" },
    ],
  },

  // ── Ship Tiers ──
  ship_tiers: {
    text: "Now yer talkin'! Every pirate starts with a Sloop \u2014 nimble but small. Ye can upgrade to a Brigantine and then a mighty Galleon at the Shipyard! Each class adds 2 more component slots, a bigger hull, and looks far more intimidatin'.",
    responses: [
      { label: "What does it cost?", next: "ship_tiers_cost" },
      { label: "Do I lose my ship class when I die?", next: "ship_tiers_death" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  ship_tiers_cost: {
    text: "It ain't cheap! The Brigantine runs ye 300 Wood, 250 Iron, 150 Fish, 100 Tea, and 2,000 Gold. The Galleon? Even steeper \u2014 400 Wood, 300 Iron, 150 Fish, 100 Tea, and 5,000 Gold. Fill yer hold to the brim!",
    responses: [
      { label: "Do I keep it when I die?", next: "ship_tiers_death" },
      { label: "That's pricey! What else?", next: "root" },
    ],
  },
  ship_tiers_death: {
    text: "Here's the good news \u2014 yer ship class is PERMANENT! Unlike components, it survives death. Once ye upgrade, ye keep that bigger ship forever. It's the best investment a pirate can make!",
    responses: [{ label: "I'm saving up! What else?", next: "root" }],
  },

  // ── Overburdened ──
  overburdened: {
    text: "When yer hold is stuffed near capacity, yer ship gets heavy. You'll see a 'Heavy' warning on screen \u2014 that means you're overburdened, sailor!",
    responses: [
      { label: "What happens when I'm heavy?", next: "overburdened_effects" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  overburdened_effects: {
    text: "A heavy ship turns slower and handles like a brick. Ye'll struggle to dodge enemies and navigate tight waters. Worse, ye can't collect any more resources until ye free up space.\n\nSell goods at a port, spend resources on components, or dump what ye don't need. A nimble ship is a livin' ship!",
    responses: [{ label: "I'll keep that in mind! What else?", next: "root" }],
  },

  // ── Leaderboard ──
  leaderboard: {
    text: "See that list on the left side of yer screen? That's the leaderboard! It ranks every sailor on the server by their Trophy count.",
    responses: [
      { label: "How do I get trophies?", next: "leaderboard_trophies" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  leaderboard_trophies: {
    text: "Trophies are earned through glory \u2014 sinkin' other players, completin' challenges, and provin' yer worth on the high seas. The more ye have, the higher ye climb.\n\nFair warning though: when ye die, ye lose half yer trophies just like everything else. So stay alive if ye want to stay on top!",
    responses: [{ label: "I'll aim for the top! What else?", next: "root" }],
  },

  // ── Death ──
  death: {
    text: "Death ain't the end, but it bites! When yer ship sinks, ye lose half yer inventory and ALL equipped components. Ye'll respawn after a short wait at a random spot with basic supplies.",
    responses: [
      { label: "How can I protect my stuff?", next: "death_protect" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  death_protect: {
    text: "Smart thinkin'! Build a vault at a port and stash yer valuables there \u2014 everything in the vault survives death! Also, spend yer resources before headin' into danger. Buy components, trade at ports, invest in upgrades. Resources sittin' in yer hold are resources ye could lose!",
    responses: [
      { label: "Tell me about the vault", next: "vault" },
      { label: "Good advice! What else?", next: "root" },
    ],
  },

  // ── Ports ──
  ports: {
    text: "Ports are yer safe haven! When docked, ye can't take damage. Perfect for catchin' yer breath after a rough fight or a long sail.",
    responses: [
      { label: "What can I do here?", next: "ports_features" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  ports_features: {
    text: "Plenty! The Market for buyin' and sellin' goods, the Shipyard for components and repairs, the Vault for storin' yer treasures, and me \u2014 yer humble guide! Each port has different prices, so it pays to explore.",
    responses: [{ label: "Thanks! What else can I learn?", next: "root" }],
  },

  // ── Vault ──
  vault: {
    text: "Ah, the vault! Every smart pirate needs a safe place for their loot. Ye can build one vault at any port on the map. Once built, it stays there \u2014 visit that port anytime to stash yer goods!",
    responses: [
      { label: "How do I build one?", next: "vault_build" },
      { label: "What can I store?", next: "vault_store" },
      { label: "Can I upgrade it?", next: "vault_upgrade" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  vault_build: {
    text: "Head to the Vault tab when docked at any port. If ye haven't built one yet, you'll see the option right there. It costs some Wood, Iron, and Gold to construct \u2014 but it's worth every coin!\n\nRemember: ye only get ONE vault across all ports, so pick yer location wisely!",
    responses: [
      { label: "What can I store?", next: "vault_store" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  vault_store: {
    text: "Anything ye'd hate to lose! Wood, Iron, Fish, Tea, Cannonballs, Trophies, and of course \u2014 Gold. Items in yer vault survive death, so it's the safest place for yer valuables.\n\nQuick quiz \u2014 what happens to items in yer vault when ye die?",
    responses: [
      { label: "They're safe!", next: "vault_store_right" },
      { label: "I lose half of them", next: "vault_store_wrong" },
      { label: "They're all gone", next: "vault_store_wrong" },
      { label: "Ask about something else", next: "root" },
    ],
  },
  vault_store_right: {
    text: "That's right! Yer vault is untouchable. Even if ye sink to the bottom of the sea, everything inside stays safe and sound. Stash before ye sail into danger!",
    responses: [{ label: "What else can I learn?", next: "root" }],
  },
  vault_store_wrong: {
    text: "Nay, yer vault is completely safe! That's the whole point, love. Everything ye deposit stays there no matter what happens to ye out on the water. It's the one thing death can't take!",
    responses: [{ label: "That's a relief! What else?", next: "root" }],
  },
  vault_upgrade: {
    text: "Yer vault starts small, but ye can upgrade it up to 5 levels! Each level increases how many items and how much gold it can hold. The catch? Each upgrade costs exponentially more Wood, Iron, and Gold.\n\nLevel 1 holds 50 items and 500 gold. By level 5, ye can store 2,500 items and 75,000 gold!\n\nHere's a handy trick \u2014 when upgradin', the game pulls resources from yer inventory first, then dips into the vault for the rest. So ye can stash yer upgrade materials right in the vault itself!",
    responses: [
      { label: "How do I build one?", next: "vault_build" },
      { label: "Ask about something else", next: "root" },
    ],
  },
};
