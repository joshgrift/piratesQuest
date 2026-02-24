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
    componentCapacity: 4,
    isCreative: false,
    vault: null,
    ...overrides,
  };
}

export function makeVaultState(overrides?: Partial<VaultState>): VaultState {
  return {
    portName: "Tortuga",
    level: 1,
    items: {},
    isHere: true,
    itemCapacity: 50,
    goldCapacity: 500,
    ...overrides,
  };
}
