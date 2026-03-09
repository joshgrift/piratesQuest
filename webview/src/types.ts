// ── Godot → React payloads (pushed via eval) ───────────────────────

/** Full port state sent when opening or updating the port UI. */
export interface PortState {
  isInPort: boolean;
  portName: string;
  itemsForSale: ShopItem[];
  inventory: Record<string, number>;
  components: ComponentData[];
  ownedComponents: OwnedComponent[];
  stats: Record<string, number>;
  health: number;
  maxHealth: number;
  componentCapacity: number;
  shipTier: number;
  shipTiers: ShipTierData[];
  isCreative: boolean;
  /** Gameplay costs sent from C# (single source of truth). */
  costs: PortCosts;
  /** Null when the player hasn't built a vault yet. */
  vault: VaultState | null;
  tavern: TavernState;
  crew: CrewState;
  leaderboard: LeaderboardEntry[];
}

export interface LeaderboardEntry {
  nickname: string;
  trophies: number;
  isLocal: boolean;
}

export interface PortCosts {
  vaultBuild: Record<string, number>;
  /** Null when no next upgrade is available (no vault or already max). */
  vaultUpgrade: Record<string, number> | null;
  repair: RepairCosts;
}

export interface RepairCosts {
  woodPerHp: number;
  fishPerHp: number;
}

/** The player's vault snapshot pushed from Godot. */
export interface VaultState {
  portName: string;
  level: number;
  items: Record<string, number>;
  /** True when the player is docked at the port where their vault is. */
  isHere: boolean;
  itemCapacity: number;
  goldCapacity: number;
}

/** Tavern state for the currently docked port. */
export interface TavernState {
  /** Characters physically present at the current port. */
  characters: TavernCharacter[];
}

/** Player crew state (available at sea and in port). */
export interface CrewState {
  crewSlots: number;
  hiredCharacterIds: string[];
  /** Detailed data for currently hired crew. */
  characters: TavernCharacter[];
}

export interface TavernCharacter {
  id: string;
  name: string;
  role: string;
  /** Filename in /images/characters/ */
  portrait: string;
  hireable: boolean;
  statChanges: StatChange[];
}

/** A tradeable item at this port with buy and/or sell prices. */
export interface ShopItem {
  type: string;
  buyPrice: number;
  sellPrice: number;
}

/** Ship tier data pushed from Godot. */
export interface ShipTierData {
  name: string;
  description: string;
  componentSlots: number;
  cost: Record<string, number>;
}

/** A ship component definition from GameData. */
export interface ComponentData {
  name: string;
  description: string;
  /** Filename in /icons/components/ (e.g. "acceleration.png") */
  icon: string;
  cost: Record<string, number>;
  statChanges: StatChange[];
}

/** A single stat modification applied by a component. */
export interface StatChange {
  stat: string;
  modifier: "Additive" | "Multiplicative";
  value: number;
}

/** A component the player owns, possibly equipped. */
export interface OwnedComponent {
  name: string;
  isEquipped: boolean;
}

// ── React → Godot IPC messages (sent via window.ipc.postMessage) ──

/** Discriminated union of all messages the UI can send to Godot. */
export type IpcMessage =
  | { action: "ready" }
  // ── Input forwarding (webview → Godot) ──────────────────────────────────
  | { action: "input_key"; key: string; pressed: boolean }
  | { action: "input_camera_rotate"; deltaX: number; deltaY: number }
  | { action: "input_camera_zoom"; delta: number }
  | { action: "input_camera_pan"; deltaX: number }
  | { action: "buy_items"; items: { type: string; quantity: number }[] }
  | { action: "sell_items"; items: { type: string; quantity: number }[] }
  | { action: "purchase_component"; name: string }
  | { action: "equip_component"; name: string }
  | { action: "unequip_component"; name: string }
  | { action: "heal" }
  | { action: "upgrade_ship" }
  | { action: "hire_character"; characterId: string }
  | { action: "fire_character"; characterId: string }
  | { action: "focus_parent" }
  | { action: "set_inventory"; items: { type: string; quantity: number }[] }
  | { action: "clear_components" }
  | { action: "set_health"; health: number }
  | { action: "build_vault" }
  | { action: "upgrade_vault" }
  | { action: "vault_deposit"; items: { type: string; quantity: number }[] }
  | { action: "vault_withdraw"; items: { type: string; quantity: number }[] }
  | { action: "set_ship_tier"; tier: number }
  | { action: "set_vault"; portName: string; level: number }
  | { action: "delete_vault" };

// ── Window augmentation for godot_wry bridge ───────────────────────

declare global {
  interface Window {
    /** godot_wry IPC channel — only available inside the native webview. */
    ipc?: { postMessage: (json: string) => void };

    /** Called by Godot when the player docks at a port. */
    openPort?: (data: PortState) => void;

    /** Called by Godot when the player leaves the port. */
    closePort?: () => void;

    /** Called by Godot after any action to push refreshed state. */
    updateState?: (data: PortState) => void;
  }
}
