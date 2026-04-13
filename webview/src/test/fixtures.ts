import type {
  PortState,
  ShopItem,
  ComponentData,
  OwnedComponent,
  StatChange,
  VaultState,
} from "../types";

// ── Factory functions ────────────────────────────────────────────────
// These create realistic test data with sensible defaults.
// Pass an overrides object to customise any field for a specific test.

export function makeStatChange(overrides?: Partial<StatChange>): StatChange {
  return {
    stat: "Speed",
    modifier: "Additive",
    value: 2,
    ...overrides,
  };
}

export function makeShopItem(overrides?: Partial<ShopItem>): ShopItem {
  return {
    type: "Wood",
    buyPrice: 10,
    sellPrice: 5,
    ...overrides,
  };
}

export function makeComponentData(
  overrides?: Partial<ComponentData>,
): ComponentData {
  return {
    name: "Iron Hull",
    description: "A sturdy iron hull that increases your ship's durability.",
    icon: "speed.png",
    cost: { Iron: 5, Coin: 20 },
    statChanges: [makeStatChange()],
    ...overrides,
  };
}

export function makeOwnedComponent(
  overrides?: Partial<OwnedComponent>,
): OwnedComponent {
  return {
    name: "Iron Hull",
    isEquipped: false,
    ...overrides,
  };
}

export function makePortState(overrides?: Partial<PortState>): PortState {
  return {
    isInPort: true,
    playerName: "Captain You",
    portName: "Tortuga",
    itemsForSale: [
      makeShopItem(),
      makeShopItem({ type: "Fish", buyPrice: 8, sellPrice: 3 }),
    ],
    inventory: { Wood: 10, Coin: 50, Fish: 5 },
    components: [makeComponentData()],
    ownedComponents: [],
    stats: { Speed: 5, Acceleration: 3, ShipCapacity: 1000 },
    health: 80,
    maxHealth: 100,
    cannonReady: true,
    cannonCooldownRemaining: 0,
    isOverburdened: false,
    componentCapacity: 4,
    shipTier: 0,
    shipTiers: [
      { name: "Sloop", description: "A nimble starter vessel", componentSlots: 4, cost: {} },
      { name: "Brigantine", description: "A sturdy mid-size warship", componentSlots: 6, cost: { Wood: 300, Iron: 250, Fish: 150, Tea: 100, Coin: 2000 } },
      { name: "Galleon", description: "A fearsome capital ship", componentSlots: 8, cost: { Wood: 400, Iron: 300, Fish: 150, Tea: 100, Coin: 5000 } },
    ],
    isCreative: false,
    costs: {
      vaultBuild: { Wood: 50, Iron: 25, Coin: 100 },
      vaultUpgrade: null,
      repair: { woodPerHp: 5, fishPerHp: 1 },
    },
    vault: null,
    tavern: {
      characters: [
        {
          id: "gideon-gearlock",
          name: "Gideon Gearlock",
          role: "Merchant Broker",
          portrait: "character8.png",
          hireable: true,
          talkPhrases: ["Mind the margins."],
          hireText: "Give me a berth and I will sharpen your prices.",
          fireText: "Very well. Keep your own books.",
          statChanges: [{ stat: "SellPriceBonus", modifier: "Additive", value: 0.005 }],
        },
        {
          id: "tommy-fuse",
          name: "Tommy Fuse",
          role: "Powder Runner",
          portrait: "character7.png",
          hireable: true,
          talkPhrases: ["Dry powder, quick hands."],
          hireText: "Say the word and I am aboard.",
          fireText: "I will find another deck for my powder tricks.",
          statChanges: [{ stat: "AttackRange", modifier: "Additive", value: 3 }],
        },
        {
          id: "valora-rumwhisper",
          name: "Valora Rumwhisper",
          role: "Rumor Broker",
          portrait: "character15.png",
          hireable: false,
          talkPhrases: ["Rumors beat blind sailing."],
          statChanges: [],
        },
      ],
    },
    crew: {
      crewSlots: 2,
      hiredCharacterIds: [],
      characters: [],
    },
    quests: {
      available: [
        {
          id: "hire_gideon_gearlock",
          title: "Earn Gideon Gearlock's Trust",
          giverNpcId: "gideon-gearlock",
          giverName: "Gideon Gearlock",
          giverPortrait: "character8.png",
          giverPortName: "Tortuga",
          revealGiverInQuestLog: true,
          canAcceptFromQuestLog: false,
          canCancel: true,
          offerText: "I can sharpen your sale prices, but first I need proof you can make a real trade run. Earn 60 gold, then return and talk to me.",
          acceptedText: "Earn 60 gold from trading, then come back and speak with me.",
          description: "Gideon will join your crew once you prove you can trade for profit.",
          completionText: "Those numbers look respectable. I am aboard.",
          rewardCrewNpcId: "gideon-gearlock",
          unlocks: [],
          steps: [
            {
              label: "Close a sale worth 60 gold",
              currentValue: 0,
              requiredValue: 60,
              isComplete: false,
            },
            {
              label: "Talk to Gideon Gearlock",
              currentValue: 0,
              requiredValue: 1,
              isComplete: false,
            },
          ],
        },
      ],
      active: null,
      all: [
        {
          id: "hire_gideon_gearlock",
          title: "Earn Gideon Gearlock's Trust",
          giverNpcId: "gideon-gearlock",
          giverName: "Gideon Gearlock",
          giverPortrait: "character8.png",
          giverPortName: "Tortuga",
          revealGiverInQuestLog: true,
          canAcceptFromQuestLog: false,
          canCancel: true,
          offerText: "I can sharpen your sale prices, but first I need proof you can make a real trade run. Earn 60 gold, then return and talk to me.",
          acceptedText: "Earn 60 gold from trading, then come back and speak with me.",
          description: "Gideon will join your crew once you prove you can trade for profit.",
          completionText: "Those numbers look respectable. I am aboard.",
          rewardCrewNpcId: "gideon-gearlock",
          unlocks: [],
          steps: [
            {
              label: "Close a sale worth 60 gold",
              currentValue: 0,
              requiredValue: 60,
              isComplete: false,
            },
            {
              label: "Talk to Gideon Gearlock",
              currentValue: 0,
              requiredValue: 1,
              isComplete: false,
            },
          ],
        },
      ],
      completedIds: [],
      recentlyCompletedIds: [],
      unlockedFeatures: [
        "SellGoods",
        "TavernTalk",
        "BuyGoods",
        "ShipyardComponents",
        "ShipTierUpgrades",
        "Vault",
      ],
    },
    serverStateJson: JSON.stringify({
      Progress: {
        Lifetime: {
          PortsVisitedCount: 7,
          PortsVisited: ["Tortuga", "Nassau", "Port Royal"],
          CannonballsShot: 24,
          ShipsHit: 11,
          ShipsSunk: 2,
          HighestShipTierReached: 1,
          TotalMoneyEarned: 940,
          TotalMoneySpent: 410,
          ItemsCollected: { Wood: 30, Fish: 14, Iron: 9 },
          ItemsBought: { CannonBall: 20, Tea: 3 },
          ItemsSold: { Fish: 8, Tea: 3 },
          SoldProfit: { Fish: 18, Tea: 21 },
          BoughtComponentNames: ["Iron Hull", "Deck Cannon"],
          HiredCrewIds: ["gideon-gearlock"],
          TalkedToNpcIds: ["scarlett", "gideon-gearlock", "valora-rumwhisper"],
        },
        CompletedQuestIds: ["scarlett-first-port"],
      },
    }),
    leaderboard: [
      { captainName: "Captain Flint", inventoryGold: 140, vaultGold: 920, totalGold: 1060 },
      { captainName: "Captain You", inventoryGold: 80, vaultGold: 340, totalGold: 420 },
      { captainName: "Blackbeard", inventoryGold: 150, vaultGold: 40, totalGold: 190 },
    ],
    ...overrides,
  };
}

export function makeVaultState(overrides?: Partial<VaultState>): VaultState {
  return {
    portId: "tortuga",
    portName: "Tortuga",
    level: 1,
    items: {},
    isHere: true,
    itemCapacity: 50,
    goldCapacity: 500,
    ...overrides,
  };
}
